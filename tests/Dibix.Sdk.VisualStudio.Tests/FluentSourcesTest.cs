﻿/*------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//----------------------------------------------------------------------------*/
using System.CodeDom.Compiler;
using Dibix;

namespace This.Is.A.Custom.Namespace
{
    #region Accessor
    [DatabaseAccessor]
    [GeneratedCodeAttribute("Dibix.Sdk", "1.0.0.0")]
    internal static class Accessor
    {
        // dbx_tests_sources_includednested
        public const string dbx_tests_sources_includednestedCommandText = @"";

        // dbx_tests_sources_excludednested
        public const string dbx_tests_sources_excludednestedCommandText = @"";

        // dbx_tests_sources_externalsp
        public const string dbx_tests_sources_externalspCommandText = @"[dbo].[dbx_tests_externalsp]";

        // DeleteProject
        public const string DeleteProjectCommandText = @"[catalog].[delete_project]";

        public static void dbx_tests_sources_includednested(this IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                accessor.Execute(dbx_tests_sources_includednestedCommandText);
            }
        }

        public static void dbx_tests_sources_excludednested(this IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                accessor.Execute(dbx_tests_sources_excludednestedCommandText);
            }
        }

        public static void dbx_tests_sources_externalsp(this IDatabaseAccessorFactory databaseAccessorFactory, int x)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                IParametersVisitor @params = accessor.Parameters()
                                                     .SetFromTemplate(new
                                                     {
                                                         x
                                                     })
                                                     .Build();
                accessor.Execute(dbx_tests_sources_externalspCommandText, System.Data.CommandType.StoredProcedure, @params);
            }
        }

        public static void DeleteProject(this IDatabaseAccessorFactory databaseAccessorFactory, string folder_name, string project_name)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                IParametersVisitor @params = accessor.Parameters()
                                                     .SetFromTemplate(new
                                                     {
                                                         folder_name,
                                                         project_name
                                                     })
                                                     .Build();
                accessor.Execute(DeleteProjectCommandText, System.Data.CommandType.StoredProcedure, @params);
            }
        }
    }
    #endregion
}