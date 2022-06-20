﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dibix.Http.Client;

namespace Dibix.Testing.Http
{
    internal static class HttpServiceFactory
    {
        public static TService CreateServiceInstance<TService>(IHttpClientFactory httpClientFactory, IHttpAuthorizationProvider authorizationProvider) => CreateServiceInstance<TService>
        (
            new KeyValuePair<Type, object>(typeof(IHttpClientFactory), httpClientFactory)
          , new KeyValuePair<Type, object>(typeof(IHttpAuthorizationProvider), authorizationProvider)
        );
        public static TService CreateServiceInstance<TService>(IHttpClientFactory httpClientFactory) => CreateServiceInstance<TService>
        (
            new KeyValuePair<Type, object>(typeof(IHttpClientFactory), httpClientFactory)
        );

        private static TService CreateServiceInstance<TService>(params KeyValuePair<Type, object>[] args)
        {
            ICollection<KeyValuePair<Type, object>> normalizedArgs = args.Concat(EnumerableExtensions.Create(new KeyValuePair<Type, object>(typeof(string), TestHttpClientConfiguration.HttpClientName))).ToArray();
            Type contractType = typeof(TService);
            Type implementationType = ResolveImplementationType(contractType);
            Type[] constructorSignature = normalizedArgs.Select(x => x.Key).ToArray();
            ConstructorInfo constructor = ResolveConstructor(implementationType, constructorSignature);
            object[] ctorArgs = normalizedArgs.Select(x => x.Value).ToArray();
            TService service = (TService)constructor.Invoke(ctorArgs);
            return service;
        }

        private static Type ResolveImplementationType(Type contractType)
        {
            foreach (Type type in contractType.Assembly.GetTypes())
            {
                HttpServiceAttribute attribute = type.GetCustomAttribute<HttpServiceAttribute>();
                if (attribute?.ContractType == contractType)
                    return type;
            }

            throw new InvalidOperationException($"Could not determine server implementation for type '{contractType}'. Is it a HTTP service generated by Dibix?");
        }

        private static ConstructorInfo ResolveConstructor(Type implementationType, ICollection<Type> constructorSignature)
        {
            foreach (ConstructorInfo constructor in implementationType.GetConstructors())
            {
                if (constructorSignature.SequenceEqual(constructor.GetParameters().Select(x => x.ParameterType)))
                    return constructor;
            }

            throw new InvalidOperationException($"Could not find constructor ({String.Join(", ", constructorSignature.Select(x => x.ToString()))}) on type: {implementationType}");
        }
    }
}