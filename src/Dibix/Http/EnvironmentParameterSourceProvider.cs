using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Dibix.Http
{
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
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
            this.MachineName = Environment.MachineName;
            this.CurrentProcessId = Process.GetCurrentProcess().Id;
        }
    }
}