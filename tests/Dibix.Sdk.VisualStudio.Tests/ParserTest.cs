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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dibix;

namespace Dibix.Sdk.VisualStudio.Tests
{
    #region Accessor
    [DatabaseAccessor]
    [GeneratedCodeAttribute("Dibix.Sdk", "1.0.0.0")]
    internal static class ParserTest
    {
        // dbx_tests_parser_invalidcolumnsforentity
        public const string dbx_tests_parser_invalidcolumnsforentityCommandText = @"SELECT COUNT(*) AS [column]
FROM (VALUES (1)) AS x(a);

WITH [x]
AS (SELECT 1 AS [i])
SELECT [i] AS [y]
FROM [x];

DECLARE @true AS BIT = 1;

IF @true = 1
    BEGIN
        SELECT 1 AS [action];
    END
ELSE
    SELECT 4 AS [action];

MERGE INTO dbo.dbx_table
 AS target
USING dbo.dbx_table AS source ON (@true = 0)
WHEN NOT MATCHED BY SOURCE THEN DELETE OUTPUT $ACTION AS [action];

SELECT 1 AS id,
       N'Cake' AS [name],
       12 AS [age]
UNION ALL
SELECT 2 AS id,
       N'Cookie' AS [name],
       16 AS [age];";

        // dbx_tests_parser_nestedifs
        public const string dbx_tests_parser_nestedifsCommandText = @"DECLARE @true AS BIT = 1;

IF @true = 1
    BEGIN
        IF @true = 0
            SELECT 1.0 AS [action];
        ELSE
            SELECT 1.1 AS [action];
    END
ELSE
    IF @true = 1
        BEGIN
            DECLARE @x AS INT = 1;
            SELECT 2 AS [action];
        END
    ELSE
        BEGIN
            SELECT 3 AS [action];
        END

IF @true = 1
    SELECT 1;
ELSE
    IF @true = 2
        SELECT 2;
    ELSE
        SELECT 3;";

        // dbx_tests_parser_nobeginend
        public const string dbx_tests_parser_nobeginendCommandText = @"SELECT 1;

SELECT @param1;

SELECT 2;";

        // dbx_tests_parser_typenames
        public const string dbx_tests_parser_typenamesCommandText = @"SELECT 0 AS [x];

SELECT 0 AS [x];

SELECT 0 AS [x];

SELECT 0 AS [x];

SELECT 0 AS [x];

SELECT 0 AS [x];

SELECT 0 AS [x],
       0 AS [x],
       0 AS [x];

SELECT 0 AS [x],
       0 AS [x],
       0 AS [x];";

        // dbx_tests_parser_unionreturn
        public const string dbx_tests_parser_unionreturnCommandText = @"(SELECT 1)
UNION ALL
(SELECT 2);";

        // dbx_tests_parser_xmlparam
        public const string dbx_tests_parser_xmlparamCommandText = @"";

        public static dbx_tests_parser_invalidcolumnsforentityResult dbx_tests_parser_invalidcolumnsforentity(this IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                using (IMultipleResultReader reader = accessor.QueryMultiple(dbx_tests_parser_invalidcolumnsforentityCommandText))
                {
                    dbx_tests_parser_invalidcolumnsforentityResult result = new dbx_tests_parser_invalidcolumnsforentityResult();
                    result.A.ReplaceWith(reader.ReadMany<int?>());
                    result.B.ReplaceWith(reader.ReadMany<int>());
                    result.C.ReplaceWith(reader.ReadMany<string>());
                    result.D.ReplaceWith(reader.ReadMany<Dibix.Sdk.VisualStudio.Tests.Direction?>());
                    result.E.ReplaceWith(reader.ReadMany<Dibix.Sdk.VisualStudio.Tests.SpecialEntity>());
                    return result;
                }
            }
        }

        public static dbx_tests_parser_nestedifsResult dbx_tests_parser_nestedifs(this IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                using (IMultipleResultReader reader = accessor.QueryMultiple(dbx_tests_parser_nestedifsCommandText))
                {
                    dbx_tests_parser_nestedifsResult result = new dbx_tests_parser_nestedifsResult();
                    result.A.ReplaceWith(reader.ReadMany<int>());
                    result.B.ReplaceWith(reader.ReadMany<int>());
                    return result;
                }
            }
        }

        public static dbx_tests_parser_nobeginendResult dbx_tests_parser_nobeginend(this IDatabaseAccessorFactory databaseAccessorFactory, Dibix.Sdk.VisualStudio.Tests.Direction param1)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                IParametersVisitor @params = accessor.Parameters()
                                                     .SetFromTemplate(new
                                                     {
                                                         param1
                                                     })
                                                     .Build();
                using (IMultipleResultReader reader = accessor.QueryMultiple(dbx_tests_parser_nobeginendCommandText, @params))
                {
                    dbx_tests_parser_nobeginendResult result = new dbx_tests_parser_nobeginendResult();
                    result.A.ReplaceWith(reader.ReadMany<int>());
                    result.B = reader.ReadSingle<int>();
                    result.C.ReplaceWith(reader.ReadMany<int>());
                    return result;
                }
            }
        }

        public static dbx_tests_parser_typenamesResult dbx_tests_parser_typenames(this IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                using (IMultipleResultReader reader = accessor.QueryMultiple(dbx_tests_parser_typenamesCommandText))
                {
                    dbx_tests_parser_typenamesResult result = new dbx_tests_parser_typenamesResult();
                    result.A.ReplaceWith(reader.ReadMany<string>());
                    result.B.ReplaceWith(reader.ReadMany<int?>());
                    result.C.ReplaceWith(reader.ReadMany<Dibix.Sdk.VisualStudio.Tests.Direction>());
                    result.D.ReplaceWith(reader.ReadMany<Dibix.Sdk.VisualStudio.Tests.Direction?>());
                    result.E.ReplaceWith(reader.ReadMany<Dibix.Sdk.VisualStudio.Tests.Direction>());
                    result.F.ReplaceWith(reader.ReadMany<Dibix.Sdk.VisualStudio.Tests.Direction?>());
                    result.G.ReplaceWith(reader.ReadMany<string, int?, Dibix.Sdk.VisualStudio.Tests.Direction>(ParserTestUtility.Map, "x,x"));
                    result.H.ReplaceWith(reader.ReadMany<Dibix.Sdk.VisualStudio.Tests.Direction?, Dibix.Sdk.VisualStudio.Tests.Direction, Dibix.Sdk.VisualStudio.Tests.Direction?>(ParserTestUtility.Map, "x,x"));
                    return result;
                }
            }
        }

        public static IEnumerable<int> dbx_tests_parser_unionreturn(this IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                return accessor.QueryMany<int>(dbx_tests_parser_unionreturnCommandText);
            }
        }

        public static void dbx_tests_parser_xmlparam(this IDatabaseAccessorFactory databaseAccessorFactory, System.Xml.Linq.XElement x)
        {
            using (IDatabaseAccessor accessor = databaseAccessorFactory.Create())
            {
                IParametersVisitor @params = accessor.Parameters()
                                                     .SetFromTemplate(new
                                                     {
                                                         x
                                                     })
                                                     .Build();
                accessor.Execute(dbx_tests_parser_xmlparamCommandText, @params);
            }
        }
    }
    #endregion

    #region Grid result types
    internal sealed class dbx_tests_parser_invalidcolumnsforentityResult
    {
        public ICollection<int?> A { get; private set; }
        public ICollection<int> B { get; private set; }
        public ICollection<string> C { get; private set; }
        public ICollection<Dibix.Sdk.VisualStudio.Tests.Direction?> D { get; private set; }
        public ICollection<Dibix.Sdk.VisualStudio.Tests.SpecialEntity> E { get; private set; }

        public dbx_tests_parser_invalidcolumnsforentityResult()
        {
            this.A = new Collection<int?>();
            this.B = new Collection<int>();
            this.C = new Collection<string>();
            this.D = new Collection<Dibix.Sdk.VisualStudio.Tests.Direction?>();
            this.E = new Collection<Dibix.Sdk.VisualStudio.Tests.SpecialEntity>();
        }
    }

    internal sealed class dbx_tests_parser_nestedifsResult
    {
        public ICollection<int> A { get; private set; }
        public ICollection<int> B { get; private set; }

        public dbx_tests_parser_nestedifsResult()
        {
            this.A = new Collection<int>();
            this.B = new Collection<int>();
        }
    }

    internal sealed class dbx_tests_parser_nobeginendResult
    {
        public ICollection<int> A { get; private set; }
        public int B { get; set; }
        public ICollection<int> C { get; private set; }

        public dbx_tests_parser_nobeginendResult()
        {
            this.A = new Collection<int>();
            this.C = new Collection<int>();
        }
    }

    internal sealed class dbx_tests_parser_typenamesResult
    {
        public ICollection<string> A { get; private set; }
        public ICollection<int?> B { get; private set; }
        public ICollection<Dibix.Sdk.VisualStudio.Tests.Direction> C { get; private set; }
        public ICollection<Dibix.Sdk.VisualStudio.Tests.Direction?> D { get; private set; }
        public ICollection<Dibix.Sdk.VisualStudio.Tests.Direction> E { get; private set; }
        public ICollection<Dibix.Sdk.VisualStudio.Tests.Direction?> F { get; private set; }
        public ICollection<string> G { get; private set; }
        public ICollection<Dibix.Sdk.VisualStudio.Tests.Direction?> H { get; private set; }

        public dbx_tests_parser_typenamesResult()
        {
            this.A = new Collection<string>();
            this.B = new Collection<int?>();
            this.C = new Collection<Dibix.Sdk.VisualStudio.Tests.Direction>();
            this.D = new Collection<Dibix.Sdk.VisualStudio.Tests.Direction?>();
            this.E = new Collection<Dibix.Sdk.VisualStudio.Tests.Direction>();
            this.F = new Collection<Dibix.Sdk.VisualStudio.Tests.Direction?>();
            this.G = new Collection<string>();
            this.H = new Collection<Dibix.Sdk.VisualStudio.Tests.Direction?>();
        }
    }
    #endregion
}