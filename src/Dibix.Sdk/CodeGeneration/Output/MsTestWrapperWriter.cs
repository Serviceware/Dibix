﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class MsTestWrapperWriter : OutputWriter, IWriter
    {
        protected override void Write(StringWriter writer, string @namespace, string className, CommandTextFormatting formatting, SourceArtifacts artifacts)
        {
            string output = BuildTestClass(@namespace, className, artifacts.Statements, formatting);
            writer.WriteRaw(output);
        }

        private static string BuildTestClass(string @namespace, string className, IEnumerable<SqlStatementInfo> statements, CommandTextFormatting formatting)
        {
            const string template = @"using Helpline.Infrastructure.Tests.Components.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace %namespace%
{
    [TestClass]
    public class %className% : DatabaseTestExecutor
    {
%methods%
    }
}";

            string methods = String.Join(String.Format("{0}{0}", Environment.NewLine), statements.Select(x => BuildTestMethod(x.Name, Format(x.Content, formatting), formatting)));

            return template.Replace("%namespace%", @namespace)
                           .Replace("%className%", className)
                           .Replace("%methods%", methods);
        }

        private static string BuildTestMethod(string testMethodName, string commandText, CommandTextFormatting formatting)
        {
            Match match = Regex.Match(testMethodName, @"^TC_([\d]+)_");
            if (!match.Success || match.Groups.Count < 1 || !Int32.TryParse(match.Groups[1].Value, out var testCaseId))
                throw new InvalidOperationException($@"Could not determine test case id for '{testMethodName}'.
Please make sure the file has the following format: TC_%TESTCASEID%_*");

            const string template = @"		[TestMethod]
        public void %testMethodName%()
        {
            const string commandText = %prefix%""%commandText%"";
            base.Execute(%testCaseId%, commandText);
        }";

            return template.Replace("%testMethodName%", testMethodName)
                           .Replace("%prefix%", formatting.HasFlag(CommandTextFormatting.Verbatim) ? "@" : String.Empty)
                           .Replace("%commandText%", commandText)
                           .Replace("%testCaseId%", testCaseId.ToString());
        }
    }
}