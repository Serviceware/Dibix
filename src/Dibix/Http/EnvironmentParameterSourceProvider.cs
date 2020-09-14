using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;

namespace Dibix.Http
{
    internal sealed class EnvironmentParameterSourceProvider : IHttpParameterSourceProvider
    {
        public const string SourceName = "ENV";

        public Type GetInstanceType(HttpActionDefinition action) => typeof(EnvironmentParameterSource);

        public Expression GetInstanceValue(Type instanceType, ParameterExpression requestParameter, ParameterExpression argumentsParameter, ParameterExpression dependencyProviderParameter) => Expression.New(typeof(EnvironmentParameterSource));
    }

    public sealed class EnvironmentParameterSource
    {
        public string MachineName { get; }
        public int CurrentProcessId { get; }

        public EnvironmentParameterSource()
        {
            this.MachineName = GetMachineName();
            this.CurrentProcessId = GetProcessId();
        }

        public static string GetMachineName() => Dns.GetHostEntry(String.Empty).HostName;

        public static int GetProcessId() => Process.GetCurrentProcess().Id;
    }
}