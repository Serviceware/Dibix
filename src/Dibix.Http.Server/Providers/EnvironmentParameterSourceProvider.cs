﻿using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Dibix.Http.Server
{
    public sealed class EnvironmentParameterSourceProvider : NonUserParameterSourceProvider, IHttpParameterSourceProvider
    {
        public static readonly string SourceName = EnvironmentParameterSource.SourceName;

        public override void Resolve(IHttpParameterResolutionContext context)
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

        public static string GetMachineName() => HostNameUtility.GetFullyQualifiedDomainName();

        public static int GetCurrentProcessId() => Process.GetCurrentProcess().Id;

        private static Expression BuildMethodCallExpression(string methodName) => Expression.Call(typeof(EnvironmentParameterSourceProvider), methodName, Type.EmptyTypes);
    }
}