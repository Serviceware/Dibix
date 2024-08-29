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
        public static IServiceCollection AddScopedOnce<TService>(this IServiceCollection services, Type implementationType)
        {
            VerifyServiceUnregistered(services, typeof(TService));
            return services.AddScoped(typeof(TService), implementationType);
        }
        public static IServiceCollection AddScopedOnce(this IServiceCollection services, Type serviceType)
        {
            VerifyServiceUnregistered(services, serviceType);
            return services.AddScoped(serviceType, serviceType);
        }

        public static IServiceCollection AddSingletonOnce<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService
        {
            VerifyServiceUnregistered<TService>(services);
            return services.AddSingleton<TService, TImplementation>();
        }
        public static IServiceCollection AddSingletonOnce<TService>(this IServiceCollection services) where TService : class
        {
            VerifyServiceUnregistered<TService>(services);
            return services.AddSingleton<TService>();
        }
        public static IServiceCollection AddSingletonOnce<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class
        {
            VerifyServiceUnregistered<TService>(services);
            return services.AddSingleton(implementationFactory);
        }
        public static IServiceCollection AddSingletonOnce<TService>(this IServiceCollection services, Type implementationType)
        {
            VerifyServiceUnregistered(services, typeof(TService));
            return services.AddSingleton(typeof(TService), implementationType);
        }
        public static IServiceCollection AddSingletonOnce(this IServiceCollection services, Type serviceType)
        {
            VerifyServiceUnregistered(services, serviceType);
            return services.AddSingleton(serviceType, serviceType);
        }

        private static void VerifyServiceUnregistered<TService>(IServiceCollection services) => VerifyServiceUnregistered(services, typeof(TService));
        private static void VerifyServiceUnregistered(IServiceCollection services, Type serviceType)
        {
            if (services.Any(x => x.ServiceType == serviceType))
                throw new InvalidOperationException($"Contract already registered: {serviceType}");
        }
    }
}