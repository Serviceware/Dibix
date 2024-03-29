﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.Sql;
using Dibix.Testing;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.Tests.CodeAnalysis
{
    public sealed partial class SqlCodeAnalysisRuleTests : TestBase
    {
        partial void Execute(string testName)
        {
            // Determine rule by 'back in time development' and create instance
            string resourceKey = ResourceUtility.BuildResourceKey($"CodeAnalysis.{testName}.xml");
            string expected = base.GetEmbeddedResourceContent(resourceKey);
            Type ruleType = Type.GetType($"Dibix.Sdk.CodeAnalysis.Rules.{testName},{typeof(ISqlCodeAnalysisRule).Assembly.GetName().Name}");
            SqlCodeAnalysisRuleAttribute descriptor = ruleType.GetCustomAttribute<SqlCodeAnalysisRuleAttribute>();
            string violationScriptPath = Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, "CodeAnalysis", $"dbx_codeanalysis_error_{descriptor.Id:D3}.sql");

            ICollection<TaskItem> sources = ((IEnumerable)DatabaseTestUtility.QueryProject("x:Project/x:ItemGroup/x:Build/@Include"))
                                                                             .OfType<XAttribute>()
                                                                             .Select(x => new TaskItem(x.Value) { ["FullPath"] = Path.Combine(DatabaseTestUtility.DatabaseProjectDirectory, x.Value) })
                                                                             .ToArray();

            // master.dacpac is actually required otherwise causes this error:
            // CodeAnalysis\dbx_codeanalysis_error_002.sql(84,10,84,10):error 71502:Procedure: [dbo].[dbx_codeanalysis_error_002_x] has an unresolved reference to object [dbo].[sp_executesql].
            // The master.dacpac lies in the VS IDE folder with an installed SSDT, which is not available in CI builds, therefore ignore it for now.
            ICollection<TaskItem> references = Array.Empty<TaskItem>();

            TestLogger logger = new TestLogger(base.Out, distinctErrorLogging: true);

            Sdk.CodeAnalysis.SqlCodeAnalysisConfiguration configuration = new Sdk.CodeAnalysis.SqlCodeAnalysisConfiguration
            {
                IsEmbedded = false,
                LimitDdlStatements = true,
                NamingConventionPrefix = "dbx"
            };
            configuration.Source.AddRange(sources);
            TSqlModel model = PublicSqlDataSchemaModelLoader.Load(preventDmlReferences: true, DatabaseTestUtility.DatabaseSchemaProviderName, DatabaseTestUtility.ModelCollation, sources, references, logger);
            LockEntryManager lockEntryManager = LockEntryManager.Create(reset: false, filePath: null);
            ISqlCodeAnalysisRuleEngine engine = SqlCodeAnalysisRuleEngine.Create(model, configuration, lockEntryManager, logger);
            IEnumerable<SqlCodeAnalysisError> errors = engine.Analyze(violationScriptPath, ruleType);

            string actual = GenerateXmlFromResults(errors);
            base.AssertEqual(expected, actual, extension: "xml");
        }

        private static string GenerateXmlFromResults(IEnumerable<SqlCodeAnalysisError> result)
        {
            XDocument doc = new XDocument
            (
                new XElement
                (
                    "errors"
                  , result.Select
                    (
                        x => new XElement
                        (
                            "error"
                          , new XAttribute("ruleid", x.RuleId)
                          , new XAttribute("message", x.Message)
                          , new XAttribute("line", x.Line)
                          , new XAttribute("column", x.Column)
                        )
                    )
                    .ToArray()
                )
            );

            using (TextWriter stringWriter = new Utf8StringWriter())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true, NewLineOnAttributes = true }))
                {
                    doc.Save(xmlWriter);
                }
                return stringWriter.ToString();
            }
        }

        private sealed class Utf8StringWriter : StringWriter { public override Encoding Encoding => Encoding.UTF8; }
    }
}