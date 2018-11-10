using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using Xunit;

namespace Dibix.Sdk.Tests.CodeGeneration
{
    public abstract class LintTestBase : SqlAccessorGeneratorTestBase
    {
        protected void RunLintTest(short lintErrorCode)
        {
            RunLintTest($@"Lint\dbx_lint_error_{lintErrorCode:D3}.sql", DetermineExpectedText());
        }/*
        protected void RunLintTest(string sourceFilePath)
        {
            this.RunLintTest(sourceFilePath, GetResourceByTestName());
        }*/
        private static void RunLintTest(string virtualFilePath, string expectedErrorText)
        {
            RunLintTest(virtualFilePath, x => AssertAreEqual(expectedErrorText, x));
        }
        private static void RunLintTest(string sourceFilePath, Action<string> errorHandler)
        {
            RunLintTest(sourceFilePath, (IList<CompilerError> x) =>
            {
                Assert.True(x.Any(), "No errors were returned");
                errorHandler(String.Join(Environment.NewLine, x));
            });
        }
        private static void RunLintTest(string virtualFilePath, Action<IList<CompilerError>> errorHandler)
        {
            SqlAccessorGenerator.Create(new LintTestEnvironment(errorHandler))
                                .AddSource("Dibix.Sdk.Tests.Database", x =>
                                {
                                    x.SelectFile(virtualFilePath)
                                     .SelectParser<SqlStoredProcedureParser>();
                                })
                                .SelectOutputWriter<SqlConstantsWriter>()
                                .Generate();
        }

        private class LintTestEnvironment : EmptyExecutionEnvironment
        {
            private readonly Action<IList<CompilerError>> _errorHandler;
            private readonly IList<CompilerError> _errors;

            public LintTestEnvironment(Action<IList<CompilerError>> errorHandler)
            {
                this._errorHandler = errorHandler;
                this._errors = new Collection<CompilerError>();
            }

            public override void RegisterError(string fileName, int line, int column, string errorNumber, string errorText)
            {
                fileName = fileName.Substring(TestsRootDirectory.Length + 1);
                this._errors.Add(new CompilerError(fileName, line, column, errorNumber, errorText));
            }

            public override bool ReportErrors()
            {
                this._errorHandler(this._errors);
                return this._errors.Any();
            }
        }
    }
}