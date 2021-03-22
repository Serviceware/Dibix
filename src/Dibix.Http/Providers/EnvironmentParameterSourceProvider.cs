﻿using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;

namespace Dibix.Http
{
    public sealed class EnvironmentParameterSourceProvider : IHttpParameterSourceProvider
    {
        public const string SourceName = "ENV";

        void IHttpParameterSourceProvider.Resolve(IHttpParameterResolutionContext context)
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

        public static string GetMachineName() => Dns.GetHostEntry(String.Empty).HostName;

        public static int GetCurrentProcessId() => Process.GetCurrentProcess().Id;

        private static Expression BuildMethodCallExpression(string methodName) => Expression.Call(typeof(EnvironmentParameterSourceProvider), methodName, new Type[0]);
    }
}