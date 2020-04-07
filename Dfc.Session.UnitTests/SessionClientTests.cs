using Dfc.Session.Models;
using Dfc.Session.Services;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace Dfc.Session.UnitTests
{
    public class SessionClientTests
    {
        private const string SessionName = ".dfc-session";
        private readonly ISessionIdGenerator sessionIdGenerator;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly SessionConfig config;
        private readonly ILogger<SessionClient> logger;
        private readonly ISessionClient sessionClient;
        private readonly IPartitionKeyGenerator partitionKeyGenerator;

        public SessionClientTests()
        {
            this.partitionKeyGenerator = A.Fake<IPartitionKeyGenerator>();
            this.sessionIdGenerator = A.Fake<ISessionIdGenerator>();
            this.httpContextAccessor = A.Fake<IHttpContextAccessor>();
            httpContextAccessor.HttpContext = new DefaultHttpContext();

            this.config = new SessionConfig
            {
                ApplicationName = "UnitTestAppName",
                Salt = "TestSalt",
            };
            this.logger = A.Fake<ILogger<SessionClient>>();

            this.sessionClient = new SessionClient(sessionIdGenerator, partitionKeyGenerator, httpContextAccessor, config, logger);
        }

        [Fact]
        public void NewSessionReturnsSessionObject()
        {
            // Arrange
            const string dummyPartitionKey = "dummyPartitionKey";
            var dummySession = new SessionResult
            {
                Counter = 123,
                EncodedSessionId = "EncodedSessionId",
            };
            A.CallTo(() => sessionIdGenerator.CreateSession(A<string>.Ignored, A<DateTime>.Ignored)).Returns(dummySession);
            A.CallTo(() => partitionKeyGenerator.GeneratePartitionKey(A<string>.Ignored, A<string>.Ignored)).Returns(dummyPartitionKey);

            // Act
            var session = sessionClient.NewSession();

            // Assert
            Assert.NotNull(session);
            Assert.Equal($"{config.Salt}|{dummySession.Counter}", session.Salt);
            Assert.Equal(dummySession.EncodedSessionId, session.SessionId);
            Assert.Equal(dummyPartitionKey, session.PartitionKey);
        }

        [Fact]
        public void CreateCookieThrowsExceptionWhenSessionNotValid()
        {
            // Arrange
            A.CallTo(() => sessionIdGenerator.ValidateSessionId(A<DfcUserSession>.Ignored)).Returns(false);
            var userSession = new DfcUserSession();

            // Act
            Assert.Throws<ArgumentException>(() => sessionClient.CreateCookie(userSession, true));

            // Assert
            A.CallTo(() => logger.Log(LogLevel.Warning, 0, A<FormattedLogValues>.Ignored, A<Exception>.Ignored, A<Func<object, Exception, string>>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void CreateCookieAddsSetCookieHeaderWhenSessionIsValid()
        {
            // Arrange
            A.CallTo(() => sessionIdGenerator.ValidateSessionId(A<DfcUserSession>.Ignored)).Returns(true);
            var userSession = new DfcUserSession
            {
                SessionId = "DummySessionId",
                Salt = "DummySalt",
                CreatedDate = DateTime.UtcNow,
                PartitionKey = "DummyPartitionKey",
            };

            // Act
            sessionClient.CreateCookie(userSession, true);
            var headers = httpContextAccessor.HttpContext.Response.Headers;
            var setCookieHeader = headers["Set-Cookie"][0];

            // Assert
            Assert.True(headers.Count == 1);
            Assert.Contains(SessionName, setCookieHeader, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CreateCookieAddsSetCookieHeaderWhenWithoutValidating()
        {
            // Arrange
            var userSession = new DfcUserSession
            {
                SessionId = "DummySessionId",
                Salt = "DummySalt",
                CreatedDate = DateTime.UtcNow,
                PartitionKey = "DummyPartitionKey",
            };

            // Act
            sessionClient.CreateCookie(userSession, false);
            var headers = httpContextAccessor.HttpContext.Response.Headers;
            var setCookieHeader = headers[HeaderNames.SetCookie][0];

            // Assert
            Assert.True(headers.Count == 1);
            Assert.Contains(SessionName, setCookieHeader, StringComparison.OrdinalIgnoreCase);
            A.CallTo(() => sessionIdGenerator.ValidateSessionId(A<DfcUserSession>.Ignored)).MustNotHaveHappened();
        }

        [Fact]
        public async Task TryFindSessionCodeUsesCookieSessionIdWhenItExists()
        {
            // Arrange
            var userSession = new DfcUserSession
            {
                SessionId = "DummySessionId",
                Salt = "DummySalt",
                CreatedDate = DateTime.UtcNow,
                PartitionKey = "DummyPartitionKey",
            };
            var userSessionJson = JsonConvert.SerializeObject(userSession);
            var httpContext = A.Fake<HttpContext>();
            var localHttpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            var cookies = new RequestCookieCollection(new Dictionary<string, string> { { SessionName, userSessionJson } });
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.Cookies).Returns(cookies);

            var localSessionClient = new SessionClient(sessionIdGenerator, partitionKeyGenerator, localHttpContextAccessor, config, logger);

            // Act
            var result = await localSessionClient.TryFindSessionCode().ConfigureAwait(false);

            // Assert
            Assert.Equal(userSession.GetCookieSessionId, result);
        }

        [Fact]
        public async Task TryFindSessionCodeUsesQueryStringSessionIdWhenItExists()
        {
            // Arrange
            const string expectedQueryStringValue = "qsValue";
            var httpContext = A.Fake<HttpContext>();
            var dummyQueryString = new QueryString($"?{SessionName.TrimStart('.')}={expectedQueryStringValue}");
            var localHttpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.QueryString).Returns(dummyQueryString);

            var localSessionClient = new SessionClient(sessionIdGenerator, partitionKeyGenerator, localHttpContextAccessor, config, logger);

            // Act
            var result = await localSessionClient.TryFindSessionCode().ConfigureAwait(false);

            // Assert
            Assert.Equal(expectedQueryStringValue, result);
        }

        [Fact]
        public async Task TryFindSessionCodeUsesFormDataWhenItExists()
        {
            // Arrange
            const string formDataValue = "someFormData";
            var httpContext = A.Fake<HttpContext>();
            var formData = new FormCollection(new Dictionary<string, StringValues> { { SessionName.TrimStart('.'), new StringValues(formDataValue) } });
            var localHttpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.HasFormContentType).Returns(true);
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.ReadFormAsync(CancellationToken.None)).Returns(formData);

            var localSessionClient = new SessionClient(sessionIdGenerator, partitionKeyGenerator, localHttpContextAccessor, config, logger);

            // Act
            var result = await localSessionClient.TryFindSessionCode().ConfigureAwait(false);

            // Assert
            Assert.Equal(formDataValue, result);
        }

        [Fact]
        public async Task TryFindSessionCodeUsesQueryStringDataWhenItExistsAndFormDataIsNull()
        {
            // Arrange
            const string expectedQueryStringValue = "qsValue";
            var httpContext = A.Fake<HttpContext>();
            var dummyQueryString = new QueryString($"?{SessionName.TrimStart('.')}={expectedQueryStringValue}");
            var localHttpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.QueryString).Returns(dummyQueryString);
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.HasFormContentType).Returns(true);
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.ReadFormAsync(CancellationToken.None)).Returns((IFormCollection)null);

            var localSessionClient = new SessionClient(sessionIdGenerator, partitionKeyGenerator, localHttpContextAccessor, config, logger);

            // Act
            var result = await localSessionClient.TryFindSessionCode().ConfigureAwait(false);

            // Assert
            Assert.Equal(expectedQueryStringValue, result);
        }

        [Fact]
        public async Task TryFindSessionCodeUsesFormDataWhenAllSourcesExist()
        {
            // Arrange
            const string formDataValue = "someFormData";
            const string expectedQueryStringValue = "qsValue";
            var userSession = new DfcUserSession
            {
                SessionId = "DummySessionId",
                Salt = "DummySalt",
                CreatedDate = DateTime.UtcNow,
                PartitionKey = "DummyPartitionKey",
            };
            var userSessionJson = JsonConvert.SerializeObject(userSession);
            var httpContext = A.Fake<HttpContext>();
            var dummyQueryString = new QueryString($"?{SessionName.TrimStart('.')}={expectedQueryStringValue}");
            var formData = new FormCollection(new Dictionary<string, StringValues> { { SessionName.TrimStart('.'), new StringValues(formDataValue) } });
            var cookies = new RequestCookieCollection(new Dictionary<string, string> { { SessionName, userSessionJson } });

            var localHttpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.Cookies).Returns(cookies);
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.QueryString).Returns(dummyQueryString);
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.HasFormContentType).Returns(true);
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.ReadFormAsync(CancellationToken.None)).Returns(formData);

            var localSessionClient = new SessionClient(sessionIdGenerator, partitionKeyGenerator, localHttpContextAccessor, config, logger);

            // Act
            var result = await localSessionClient.TryFindSessionCode().ConfigureAwait(false);

            // Assert
            Assert.Equal(formDataValue, result);
        }

        [Fact]
        public async Task TryFindSessionCodeUsesQueryDataWhenOnlyQueryStringAndCookieSourcesExist()
        {
            // Arrange
            const string expectedQueryStringValue = "qsValue";
            var userSession = new DfcUserSession
            {
                SessionId = "DummySessionId",
                Salt = "DummySalt",
                CreatedDate = DateTime.UtcNow,
                PartitionKey = "DummyPartitionKey",
            };
            var userSessionJson = JsonConvert.SerializeObject(userSession);
            var httpContext = A.Fake<HttpContext>();
            var dummyQueryString = new QueryString($"?{SessionName.TrimStart('.')}={expectedQueryStringValue}");
            var cookies = new RequestCookieCollection(new Dictionary<string, string> { { SessionName, userSessionJson } });

            var localHttpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.Cookies).Returns(cookies);
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.QueryString).Returns(dummyQueryString);

            var localSessionClient = new SessionClient(sessionIdGenerator, partitionKeyGenerator, localHttpContextAccessor, config, logger);

            // Act
            var result = await localSessionClient.TryFindSessionCode().ConfigureAwait(false);

            // Assert
            Assert.Equal(expectedQueryStringValue, result);
        }

        [Fact]
        public async Task TryFindSessionCodeReturnsNullWhenNoSourcesPopulated()
        {
            // Arrange
            var httpContext = A.Fake<HttpContext>();
            httpContext.Request.Host = new HostString("testhost");
            httpContext.Request.PathBase = new PathString("/pathBase");
            httpContext.Request.Path = new PathString("/path");
            httpContext.Request.Scheme = "http";
            httpContext.Request.QueryString = new QueryString("?dummyQS");
            var localHttpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            var localSessionClient = new SessionClient(sessionIdGenerator, partitionKeyGenerator, localHttpContextAccessor, config, logger);

            // Act
            var result = await localSessionClient.TryFindSessionCode().ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GeneratePartitionKeyUsesGeneratePartitionKeyWhenCalled()
        {
            const string sessionId = "DummySessionId";
            sessionClient.GeneratePartitionKey(sessionId);
            A.CallTo(() => partitionKeyGenerator.GeneratePartitionKey(A<string>.Ignored, sessionId))
                .MustHaveHappened();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ValidateUserSessionWhenCalledThenReturnsWhetherUserSessionIsValid(bool sessionIsValid)
        {
            // Arrange
            var dfcUserSession = new DfcUserSession
            {
                CreatedDate = DateTime.UtcNow,
                PartitionKey = "partitionKey",
                Salt = "salt",
                SessionId = "sessionId",
            };
            A.CallTo(() => sessionIdGenerator.ValidateSessionId(dfcUserSession)).Returns(sessionIsValid);

            // Act
            var result = sessionClient.ValidateUserSession(dfcUserSession);

            // Assert
            Assert.Equal(sessionIsValid, result);
            A.CallTo(() => sessionIdGenerator.ValidateSessionId(dfcUserSession)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void GetUserSessionFromCookieReturnsSessionWhenCookieExists()
        {
            // Arrange
            var userSession = new DfcUserSession
            {
                SessionId = "DummySessionId",
                Salt = "DummySalt",
                CreatedDate = DateTime.UtcNow,
                PartitionKey = "DummyPartitionKey",
            };
            var userSessionJson = JsonConvert.SerializeObject(userSession);
            var httpContext = A.Fake<HttpContext>();
            var localHttpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            var cookies = new RequestCookieCollection(new Dictionary<string, string> { { SessionName, userSessionJson } });
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.Cookies).Returns(cookies);

            var localSessionClient = new SessionClient(sessionIdGenerator, partitionKeyGenerator, localHttpContextAccessor, config, logger);

            // Act
            var result = localSessionClient.GetUserSessionFromCookie();

            // Assert
            Assert.Equal(userSession.GetCookieSessionId, result.GetCookieSessionId);
        }

        [Fact]
        public void GetUserSessionFromCookieReturnsNullWhenCookieDoesNotExist()
        {
            // Arrange
            var httpContext = A.Fake<HttpContext>();
            var localHttpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            var cookies = new RequestCookieCollection();
            A.CallTo(() => localHttpContextAccessor.HttpContext.Request.Cookies).Returns(cookies);

            var localSessionClient = new SessionClient(sessionIdGenerator, partitionKeyGenerator, localHttpContextAccessor, config, logger);

            // Act
            var result = localSessionClient.GetUserSessionFromCookie();

            // Assert
            Assert.Null(result);
        }
    }
}