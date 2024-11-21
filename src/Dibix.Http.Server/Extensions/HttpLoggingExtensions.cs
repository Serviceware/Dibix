using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// This solution is far from optimal. It is also highly discouraged to log all request headers.
    /// See: https://github.com/dotnet/aspnetcore/issues/40981#issuecomment-1085393133
    /// This implementation solely serves the purpose of tracking the unknown request headers during development.
    /// NOTE: This should never be used in production. Use this method to gather unknown headers and then register them before going to production.
    /// </summary>
    public static class HttpLoggingServicesExtensions
    {
        public static IServiceCollection AddHttpLoggingWithSensitiveRequestHeaders(this IServiceCollection services, IConfiguration configuration, Action<HttpLoggingOptions> configureOptions = null)
        {
            services.AddHttpLogging(configureOptions ?? (_ => { }));
            bool logSensitiveRequestHeaders = configuration.GetValue<bool>("HttpLogging:LogSensitiveRequestHeaders");
            if (!logSensitiveRequestHeaders)
                return services;

            services.AddHttpLoggingInterceptor<AllHeadersHttpLoggingInterceptor>();
            services.AddHttpContextAccessor();
            services.AddSingleton<IOptionsMonitorCache<HttpLoggingOptions>, ScopedHttpLoggingOptionsCache>();
            return services;
        }

        private sealed class ScopedHttpLoggingOptionsCache : IOptionsMonitorCache<HttpLoggingOptions>
        {
            private static readonly string ContextKey = typeof(ScopedHttpLoggingOptionsCache).FullName;
            private readonly IHttpContextAccessor _httpContextAccessor;
            private readonly ConcurrentDictionary<HttpContext, Lazy<HttpLoggingOptions>> _cache = new ConcurrentDictionary<HttpContext, Lazy<HttpLoggingOptions>>(concurrencyLevel: 1, capacity: 31);

            public ScopedHttpLoggingOptionsCache(IHttpContextAccessor httpContextAccessor)
            {
                _httpContextAccessor = httpContextAccessor;
            }

            public void Clear() => _cache.Clear();

            public HttpLoggingOptions GetOrAdd(string name, Func<HttpLoggingOptions> createOptions)
            {
                if (_httpContextAccessor.HttpContext == null)
                    return createOptions();

                if (_httpContextAccessor.HttpContext.Items.TryGetValue(ContextKey, out object value))
                    return (HttpLoggingOptions)value;

                HttpLoggingOptions instance = createOptions();
                _httpContextAccessor.HttpContext.Items.Add(ContextKey, instance);
                return instance;
            }

            public bool TryAdd(string name, HttpLoggingOptions options) => throw new NotSupportedException();

            public bool TryRemove(string name) => true;
        }

        private sealed class AllHeadersHttpLoggingInterceptor : IHttpLoggingInterceptor
        {
            private readonly IOptionsMonitor<HttpLoggingOptions> _httpLoggingOptions;

            public AllHeadersHttpLoggingInterceptor(IOptionsMonitor<HttpLoggingOptions> httpLoggingOptions)
            {
                _httpLoggingOptions = httpLoggingOptions;
            }

            public ValueTask OnRequestAsync(HttpLoggingInterceptorContext logContext)
            {
                foreach (KeyValuePair<string, StringValues> header in logContext.HttpContext.Request.Headers)
                {
                    _httpLoggingOptions.CurrentValue.RequestHeaders.Add(header.Key);
                }
                return ValueTask.CompletedTask;
            }

            public ValueTask OnResponseAsync(HttpLoggingInterceptorContext logContext)
            {
                return ValueTask.CompletedTask;
            }
        }
    }
}