using Dfc.Session.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;
using System.Web;

namespace Dfc.Session.IntegrationTests
{
    public class SessionClientTests
    {
        private const string DefaultSessionName = ".dfc-session";
        private readonly ISessionClient sessionClient;
        private readonly SessionConfig sessionConfig;
        private readonly IHttpContextAccessor contextAccessor;

        public SessionClientTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            sessionConfig = configuration.GetSection(nameof(SessionConfig)).Get<SessionConfig>();

            var serviceProvider = new ServiceCollection().AddSessionServices(sessionConfig);
            serviceProvider.AddLogging();
            serviceProvider.AddHttpContextAccessor();

            var services = serviceProvider.BuildServiceProvider();

            sessionClient = services.GetService<ISessionClient>();
            contextAccessor = services.GetService<IHttpContextAccessor>();
        }

        [Fact]
        public void NewSessionReturnsDfcUserSessionWithValuesFromConfig()
        {
            // Act
            var result = sessionClient.NewSession();

            // Assert
            Assert.StartsWith(sessionConfig.Salt, result.Salt, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(sessionConfig.ApplicationName, result.PartitionKey, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CreateCookieAddsToResponseCookies()
        {
            // Arrange
            var session = sessionClient.NewSession();
            contextAccessor.HttpContext = new DefaultHttpContext();

            // Act
            sessionClient.CreateCookie(session, true);

            var headers = contextAccessor.HttpContext.Response.Headers;
            var setCookieHeader = headers["Set-Cookie"][0];
            Assert.Contains(DefaultSessionName, setCookieHeader, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(session.SessionId, setCookieHeader, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task TryFindSessionCodeReturnsCookieValue()
        {
            // Arrange
            var userSession = new DfcUserSession
            {
                SessionId = "DummySessionId",
                Salt = "DummySalt",
                PartitionKey = "DummyPartitionKey",
            };
            var userSessionJson = JsonConvert.SerializeObject(userSession);
            var cookies = new RequestCookieCollection(new Dictionary<string, string> { { DefaultSessionName, HttpUtility.UrlEncode(userSessionJson) } });
            contextAccessor.HttpContext = new DefaultHttpContext();
            contextAccessor.HttpContext.Request.Cookies = cookies;

            var result = await sessionClient.TryFindSessionCode().ConfigureAwait(false);

            Assert.Equal(userSession.GetCookieSessionId, result);
        }

        [Fact]
        public async Task TryFindSessionCodeReturnsQueryStringValue()
        {
            // Arrange
            const string sessionValue = "queryStringSessionValue";
            var requestQueryString = new QueryString($"?{DefaultSessionName.TrimStart('.')}={sessionValue}");
            contextAccessor.HttpContext = new DefaultHttpContext();
            contextAccessor.HttpContext.Request.QueryString = requestQueryString;

            var result = await sessionClient.TryFindSessionCode().ConfigureAwait(false);

            Assert.Equal(sessionValue, result);
        }

        [Fact]
        public async Task TryFindSessionCodeReturnsFormDataValue()
        {
            // Arrange
            const string sessionValue = "formDataSessionValue";
            var formCollection = new FormCollection(new Dictionary<string, StringValues> { { DefaultSessionName.TrimStart('.'), new StringValues(sessionValue) } });
            contextAccessor.HttpContext = new DefaultHttpContext();
            contextAccessor.HttpContext.Request.ContentType = "application/x-www-form-urlencoded";
            contextAccessor.HttpContext.Request.Form = formCollection;

            var result = await sessionClient.TryFindSessionCode().ConfigureAwait(false);

            Assert.Equal(sessionValue, result);
        }
    }
}