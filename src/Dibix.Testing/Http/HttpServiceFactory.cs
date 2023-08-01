﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Dibix.Http.Client;

namespace Dibix.Testing.Http
{
    internal static class HttpServiceFactory
    {
        private static readonly Type[][] ConstructorSignatures =
        {
            new[] { typeof(IHttpClientFactory), typeof(HttpClientOptions), typeof(IHttpAuthorizationProvider), typeof(string) }
          , new[] { typeof(IHttpClientFactory), typeof(HttpClientOptions), typeof(string) }
        };

        public static TService CreateServiceInstance<TService>(IHttpClientFactory httpClientFactory, HttpClientOptions httpClientOptions, IHttpAuthorizationProvider authorizationProvider)
        {
            Type implementationType = ResolveImplementationType(typeof(TService));
            ConstructorInfo constructor = SelectConstructor(implementationType);
            IList<ParameterInfo> parameters = constructor.GetParameters();
            object[] ctorArgs = new object[parameters.Count];

            for (int i = 0; i < parameters.Count; i++)
            {
                ParameterInfo parameter = parameters[i];
                object value;
                if (parameter.ParameterType == typeof(IHttpClientFactory))
                    value = httpClientFactory;
                else if (parameter.ParameterType == typeof(HttpClientOptions))
                    value = httpClientOptions;
                else if (parameter.ParameterType == typeof(IHttpAuthorizationProvider))
                    value = authorizationProvider;
                else if (parameter.ParameterType == typeof(string))
                    value = TestHttpClientFactoryBuilder.HttpClientName;
                else
                    throw new InvalidOperationException(@$"Unexpected service constructor parameter type: {parameter.ParameterType} {parameter.Name} [{i}]
Service type: {implementationType}");

                ctorArgs[i] = value;
            }

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

        private static ConstructorInfo SelectConstructor(Type implementationType)
        {
            ICollection<ConstructorInfo> constructors = implementationType.GetConstructors();
            foreach (ConstructorInfo constructor in constructors)
            {
                foreach (Type[] constructorSignature in ConstructorSignatures)
                {
                    if (constructorSignature.SequenceEqual(constructor.GetParameters().Select(x => x.ParameterType)))
                        return constructor;
                }
            }

            string constructorSignatures = String.Join(Environment.NewLine, ConstructorSignatures.Select(x => $"- ({String.Join(", ", x.Select(y => y.ToString()))})"));
            throw new InvalidOperationException($@"Could not find a matching constructor candidate on type '{implementationType}'. Tried:
{constructorSignatures}");
        }
    }
}