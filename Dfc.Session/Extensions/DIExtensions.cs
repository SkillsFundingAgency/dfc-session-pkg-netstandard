using Dfc.Session.Models;
using Dfc.Session.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Dfc.Session
{
    [ExcludeFromCodeCoverage]
    public static class DIExtensions
    {
        public static IServiceCollection AddSessionServices(this IServiceCollection services, SessionConfig sessionConfig)
        {
            services.AddSingleton(sessionConfig);
            services.AddScoped<ISessionClient, SessionClient>();
            services.AddScoped<ISessionIdGenerator, SessionIdGenerator>();
            services.AddScoped<IPartitionKeyGenerator, PartitionKeyGenerator>();
            services.AddLogging();
            return services;
        }
    }
}