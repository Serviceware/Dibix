﻿/*------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//----------------------------------------------------------------------------*/
using System;
using System.CodeDom.Compiler;
using System.Reflection;
using Dibix;

namespace This.Is.A.Custom.Namespace
{
    [GeneratedCodeAttribute("Dibix.Sdk", "1.0.0.0")]
    internal static class Accessor
    {
        // dbx_tests_sources_includednested
        public const string dbx_tests_sources_includednestedCommandText = @"";

        // dbx_tests_sources_excludednested
        public const string dbx_tests_sources_excludednestedCommandText = @"";

        // dbx_tests_sources_externalsp
        public const string dbx_tests_sources_externalspCommandText = @"[dbo].[dbx_tests_externalsp]";

        public static int dbx_tests_sources_includednested(this IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                return accessor.Execute(dbx_tests_sources_includednestedCommandText);
            }
        }
        public static int dbx_tests_sources_excludednested(this IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                return accessor.Execute(dbx_tests_sources_excludednestedCommandText);
            }
        }
        public static int dbx_tests_sources_externalsp(this IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                return accessor.Execute(dbx_tests_sources_externalspCommandText, System.Data.CommandType.StoredProcedure);
            }
        }

        public static readonly MethodInfo dbx_tests_sources_includednestedMethodInfo = new Func<IDatabaseAccessorFactory, int>(dbx_tests_sources_includednested).Method;
        public static readonly MethodInfo dbx_tests_sources_excludednestedMethodInfo = new Func<IDatabaseAccessorFactory, int>(dbx_tests_sources_excludednested).Method;
        public static readonly MethodInfo dbx_tests_sources_externalspMethodInfo = new Func<IDatabaseAccessorFactory, int>(dbx_tests_sources_externalsp).Method;
    }
}