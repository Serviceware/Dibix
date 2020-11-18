using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;

namespace Dibix.Http
{
    internal sealed class EnvironmentParameterSourceProvider : IHttpParameterSourceProvider
    {
        public const string SourceName = "ENV";

        public void Resolve(IHttpParameterResolutionContext context)
        {
            Expression value = BuildExpression(context.PropertyPath);
            context.ResolveUsingValue(value);
        }

        private static Expression BuildExpression(string propertyName)
        {
            switch (propertyName)
            {
                case "MachineName": return BuildMethodCallExpression(nameof(GetMachineName));
                case "CurrentProcessId": return BuildMethodCallExpression(nameof(GetCurrentProcessId));
                default: throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, null);
            }
        }

        private static Expression BuildMethodCallExpression(string methodName) => Expression.Call(typeof(EnvironmentParameterSourceProvider), methodName, new Type[0]);

        private static string GetMachineName() => Dns.GetHostEntry(String.Empty).HostName;

        private static int GetCurrentProcessId() => Process.GetCurrentProcess().Id;
}
}