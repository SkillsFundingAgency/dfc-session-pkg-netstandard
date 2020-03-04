using Dfc.Session.Models;
using Dfc.Session.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Dfc.Session
{
    public class SessionClient : ISessionClient
    {
        private const string SessionName = ".dfc-session";
        private readonly ISessionIdGenerator sessionIdGenerator;
        private readonly IPartitionKeyGenerator partitionKeyGenerator;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly SessionConfig sessionConfig;
        private readonly ILogger<SessionClient> logger;

        public SessionClient(ISessionIdGenerator sessionIdGenerator, IPartitionKeyGenerator partitionKeyGenerator, IHttpContextAccessor httpContextAccessor, SessionConfig sessionConfig, ILogger<SessionClient> logger)
        {
            this.sessionIdGenerator = sessionIdGenerator;
            this.partitionKeyGenerator = partitionKeyGenerator;
            this.httpContextAccessor = httpContextAccessor;
            this.sessionConfig = sessionConfig;
            this.logger = logger;
        }

        public DfcUserSession NewSession()
        {
            var currentDate = DateTime.UtcNow;
            var sessionResult = sessionIdGenerator.CreateSession(sessionConfig.Salt, currentDate);
            var partitionKey = partitionKeyGenerator.GeneratePartitionKey(sessionConfig.ApplicationName, sessionResult.EncodedSessionId);

            return new DfcUserSession
            {
                Salt = $"{sessionConfig.Salt}|{sessionResult.Counter}",
                CreatedDate = currentDate,
                SessionId = sessionResult.EncodedSessionId,
                PartitionKey = partitionKey,
            };
        }

        public void CreateCookie(DfcUserSession userSession, bool validateSessionId)
        {
            if (validateSessionId)
            {
                if (sessionIdGenerator.ValidateSessionId(userSession))
                {
                    CreateCookie(userSession);
                }
                else
                {
                    var message = $"SessionId not valid for session '{userSession?.SessionId}'";
                    logger?.LogWarning(message);
                    throw new ArgumentException(message, nameof(userSession));
                }
            }
            else
            {
                CreateCookie(userSession);
            }
        }

        public async Task<string> TryFindSessionCode()
        {
            var request = httpContextAccessor.HttpContext.Request;
            string sessionId = null;

            var cookieSessionId = request.Cookies[SessionName];
            if (!string.IsNullOrWhiteSpace(cookieSessionId))
            {
                sessionId = cookieSessionId;
            }

            var queryDictionary = System.Web.HttpUtility.ParseQueryString(request.QueryString.ToString());
            var queryStringSessionId = queryDictionary.Get(SessionName.TrimStart('.'));

            if (!string.IsNullOrWhiteSpace(queryStringSessionId))
            {
                sessionId = queryStringSessionId;
            }

            if (request.HasFormContentType)
            {
                var formData = await request.ReadFormAsync().ConfigureAwait(false);
                var formSessionId = GetFormValue(SessionName.TrimStart('.'), formData);

                if (!string.IsNullOrWhiteSpace(formSessionId))
                {
                    sessionId = formSessionId;
                }
            }

            if (string.IsNullOrWhiteSpace(sessionId) && request.Path.ToString() != "/")
            {
                logger?.LogWarning($"Unable to get session Id in {request.GetDisplayUrl()}");
            }

            return string.IsNullOrWhiteSpace(sessionId) ? null : sessionId;
        }

        private static string GetFormValue(string key, IFormCollection formData)
        {
            if (formData == null)
            {
                return null;
            }

            formData.TryGetValue(key, out var stringValues);
            return stringValues.Count == 0 ? null : stringValues[0];
        }

        private void CreateCookie(DfcUserSession userSession)
        {
            httpContextAccessor.HttpContext.Response.Cookies.Append(SessionName, $"{userSession.PartitionKey}-{userSession.SessionId}", new CookieOptions
            {
                Secure = true,
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
            });
        }
    }
}