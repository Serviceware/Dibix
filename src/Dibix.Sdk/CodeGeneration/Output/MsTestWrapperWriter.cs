using System;
using System.Linq;
using System.Text.RegularExpressions;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class MsTestWrapperWriter : CodeGenerator
    {
        public MsTestWrapperWriter(ILogger logger) : base(logger) { }

        protected override void Write(StringWriter writer, CodeGenerationModel model)
        {
            string output = BuildTestClass(model);
            writer.WriteRaw(output);
        }

        private static string BuildTestClass(CodeGenerationModel model)
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

            string methods = String.Join(String.Format("{0}{0}", Environment.NewLine), model.Statements.Select(x => BuildTestMethod(x.Name, x.Content, model.CommandTextFormatting)));

            return template.Replace("%namespace%", model.RootNamespace)
                           .Replace("%className%", model.DefaultClassName)
                           .Replace("%methods%", methods);
        }

        private static string BuildTestMethod(string testMethodName, string commandText, CommandTextFormatting formatting)
        {
            Match match = Regex.Match(testMethodName, @"^TC_([\d]+)_");
            if (!match.Success || match.Groups.Count < 1 || !Int32.TryParse(match.Groups[1].Value, out int testCaseId))
                throw new InvalidOperationException($@"Could not determine test case id for '{testMethodName}'.
Please make sure the file has the following format: TC_%TESTCASEID%_*");

            const string template = @"		[TestMethod]
        public void %testMethodName%()
        {
            const string commandText = %prefix%""%commandText%"";
            base.Execute(%testCaseId%, commandText);
        }";

            bool verbatim = formatting == CommandTextFormatting.MultiLine;
            return template.Replace("%testMethodName%", testMethodName)
                           .Replace("%prefix%", verbatim ? "@" : String.Empty)
                           .Replace("%commandText%", CSharpStringValue.SanitizeValue(verbatim, commandText))
                           .Replace("%testCaseId%", testCaseId.ToString());
        }
    }
}