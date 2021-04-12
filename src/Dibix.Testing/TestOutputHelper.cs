﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal sealed class TestOutputHelper : TextWriter, ITestOutputHelper, IDisposable
    {
        private readonly string _outputPath;
        private readonly StreamWriter _output;
        private readonly TestContext _testContext;
        private readonly bool _isAzureDevops;
        private readonly bool _supportsFile;
        private bool _collectingLine;
        private readonly Process _tail;

        public override Encoding Encoding => Encoding.UTF8;

        public TestOutputHelper(TestContext testContext, string logFileName, bool tailOutput)
        {
            this._testContext = testContext;
            string privateResultsDirectory = GetPrivateResultsDirectory(testContext, out bool isSpecified);
            this._isAzureDevops = isSpecified;
            this._supportsFile = !String.IsNullOrEmpty(logFileName);

            if (!this._supportsFile) 
                return;

            this._outputPath = Path.Combine(privateResultsDirectory, logFileName);
            this._output = new StreamWriter(this._outputPath);

            if (!tailOutput) 
                return;

            this._tail = Process.Start(new ProcessStartInfo("powershell", $"-Command Write '{testContext.TestName}'; Get-Content '{this._outputPath}' -Wait") { UseShellExecute = true });
            if (this._tail == null || this._tail.HasExited)
                throw new InvalidOperationException("Could not tail output");

            Process.GetCurrentProcess().Exited += (_, _) => this.EndOutputTail();
        }

        public TraceListener CreateTraceListener() => new TestOutputHelperTraceListener(this);

        public override void Write(string message) => this.Write(message, appendLine: false);

        public override void WriteLine() => this.Write(message: String.Empty, appendLine: true);

        public override Task WriteLineAsync()
        {
            this.Write(message: String.Empty, appendLine: true);
            return Task.CompletedTask;
        }

        public override void WriteLine(string message) => this.Write(message, appendLine: true);

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (!this._supportsFile)
                return;

            // Unfortunately azure devops does not add the output to the test result, if it has passed.
            // Therefore we add our output to the result here.
            if (this._isAzureDevops && this._testContext.CurrentTestOutcome == UnitTestOutcome.Passed)
            {
                this._testContext.AddResultFile(this._outputPath);
            }

            this._output.Dispose();
            this.EndOutputTail();
        }

        private void EndOutputTail()
        {
            if (!this._tail.HasExited) 
                this._tail.Kill();
        }

        private void Write(string message, bool appendLine)
        {
            string[] lines = message.Split('\n')
                                    .Select(x => x.Trim('\r'))
                                    .ToArray();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (!this._collectingLine || i > 0)
                    line = $"[{DateTime.Now:O}] {line}";
                
                Console.Write(line);

                if (this._supportsFile)
                    this._output.Write(line);
            }

            if (appendLine)
            {
                Console.WriteLine();

                if (this._supportsFile)
                    this._output.WriteLine();
            }

            this._collectingLine = !appendLine;

            if (this._supportsFile)
                this._output.Flush();
        }

        private static string GetPrivateResultsDirectory(TestContext testContext, out bool isSpecified)
        {
            string privateResultsDirectory = (string)testContext.Properties["PrivateTestResultsDirectory"];
            isSpecified = !String.IsNullOrEmpty(privateResultsDirectory);
            if (!isSpecified)
                privateResultsDirectory = testContext.TestRunResultsDirectory;

            Directory.CreateDirectory(privateResultsDirectory);
            return privateResultsDirectory;
        }

        private sealed class TestOutputHelperTraceListener : TraceListener
        {
            private readonly TestOutputHelper _testOutputHelper;

            public TestOutputHelperTraceListener(TestOutputHelper testOutputHelper) => this._testOutputHelper = testOutputHelper;

            public override void Write(string message) { } // Ignore trace category prefix stuff

            public override void WriteLine(string message) => this._testOutputHelper.WriteLine(message);
        }
    }
}