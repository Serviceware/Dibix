﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dibix.Sdk
{
    public class MsTestWrapperWriter : SqlWriter, IWriter
    {
        protected override void Write(StringWriter writer, string projectName, IList<SqlStatementInfo> statements)
        {
            string output = this.BuildTestClass(base.Namespace, base.ClassName, statements, base.Formatting);
            writer.WriteRaw(output);
        }

        private string BuildTestClass(string @namespace, string className, IEnumerable<SqlStatementInfo> queries, SqlQueryOutputFormatting formatting)
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

            string methods = String.Join(String.Format("{0}{0}", Environment.NewLine), queries.Select(x => BuildTestMethod(x.Name, base.Format(x.Content), formatting)));

            return template.Replace("%namespace%", @namespace)
                           .Replace("%className%", className)
                           .Replace("%methods%", methods);
        }

        private static string BuildTestMethod(string testMethodName, string commandText, SqlQueryOutputFormatting formatting)
        {
            Match match = Regex.Match(testMethodName, @"^TC_([\d]+)_");
            int testCaseId;
            if (!match.Success || match.Groups.Count < 1 || !Int32.TryParse(match.Groups[1].Value, out testCaseId))
                throw new InvalidOperationException(String.Format(@"Could not determine test case id for '{0}'.
Please make sure the file has the following format: TC_%TESTCASEID%_*", testMethodName));

            const string template = @"		[TestMethod]
        public void %testMethodName%()
        {
            const string commandText = %prefix%""%commandText%"";
            base.Execute(%testCaseId%, commandText);
        }";

            return template.Replace("%testMethodName%", testMethodName)
                           .Replace("%prefix%", formatting.HasFlag(SqlQueryOutputFormatting.Verbatim) ? "@" : String.Empty)
                           .Replace("%commandText%", commandText)
                           .Replace("%testCaseId%", testCaseId.ToString());
        }
    }
}