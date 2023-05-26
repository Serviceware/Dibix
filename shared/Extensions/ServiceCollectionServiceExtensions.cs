using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class ServiceCollectionServiceExtensions
    {
        public static IServiceCollection AddScopedOnce<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService
        {
            VerifyServiceUnregistered<TService>(services);
            return services.AddScoped<TService, TImplementation>();
        }
        public static IServiceCollection AddScopedOnce<TService>(this IServiceCollection services) where TService : class
        {
            VerifyServiceUnregistered<TService>(services);
            return services.AddScoped<TService>();
        }
        public static IServiceCollection AddScopedOnce<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class
        {
            VerifyServiceUnregistered<TService>(services);
            return services.AddScoped(implementationFactory);
        }
        public static IServiceCollection AddScopedOnce(this IServiceCollection services, Type serviceType)
        {
            VerifyServiceUnregistered(services, serviceType);
            return services.AddScoped(serviceType, serviceType);
        }

        private static void VerifyServiceUnregistered<TService>(IServiceCollection services) => VerifyServiceUnregistered(services, typeof(TService));
        private static void VerifyServiceUnregistered(IServiceCollection services, Type serviceType)
        {
            if (services.Any(x => x.ServiceType == serviceType))
                throw new InvalidOperationException($"Contract already registered: {serviceType}");
        }
    }
}