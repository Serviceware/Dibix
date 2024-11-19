using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace Dibix.Http.Client
{
    internal static class HttpServiceConstructorSelector
    {
        private static readonly Type[][] ConstructorSignatures =
        [
            [typeof(IHttpClientFactory), typeof(HttpClientOptions), typeof(IHttpAuthorizationProvider), typeof(string)]
          , [typeof(IHttpClientFactory), typeof(HttpClientOptions), typeof(string)]
        ];

        public static ConstructorInfo SelectConstructor(Type implementationType)
        {
            foreach (ConstructorInfo constructor in implementationType.GetConstructors())
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