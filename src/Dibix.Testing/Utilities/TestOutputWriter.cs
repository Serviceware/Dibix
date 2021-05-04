using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    internal sealed class TestOutputWriter : TextWriter, IDisposable
    {
        private const string OutputFileName = "output.log";
        private readonly string _outputPath;
        private readonly StreamWriter _output;
        private readonly TestContext _testContext;
        private readonly bool _isAzureDevops;
        private readonly bool _outputToFile;
        private bool _collectingLine;
        private readonly Process _tail;

        public override Encoding Encoding => Encoding.UTF8;

        public TestOutputWriter(TestContext testContext, bool outputToFile, bool tailOutput)
        {
            this._testContext = testContext;
            this._outputToFile = outputToFile;

            string privateResultsDirectory = testContext.GetPrivateResultsDirectory(out bool isSpecified);
            this._isAzureDevops = isSpecified;

            if (!this._outputToFile) 
                return;

            this._outputPath = Path.Combine(privateResultsDirectory, OutputFileName);
            this._output = new StreamWriter(this._outputPath);

            if (!tailOutput) 
                return;

            this._tail = Process.Start(new ProcessStartInfo("powershell", $"-Command Write '{testContext.TestName}'; Get-Content '{this._outputPath}' -Wait") { UseShellExecute = true });
            if (this._tail == null || this._tail.HasExited)
                throw new InvalidOperationException("Could not tail output");

            Process.GetCurrentProcess().Exited += (sender, e) => this.EndOutputTail();
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

            if (!this._outputToFile)
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
            if (this._tail != null && !this._tail.HasExited)
                this._tail.Kill();
        }

        private void Write(string message, bool appendLine)
        {
            string[] lines = message.Split('\n')
                                    .Select(x => x.Trim('\r'))
                                    .ToArray();

            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) 
                    this.AppendLine();

                string line = lines[i];

                if (!this._collectingLine || i > 0)
                    line = $"[{DateTime.Now:O}] {line}";
                
                Console.Write(line);

                if (this._outputToFile)
                    this._output.Write(line);
            }

            if (appendLine)
                this.AppendLine();

            this._collectingLine = !appendLine;

            if (this._outputToFile)
                this._output.Flush();
        }

        private void AppendLine()
        {
            Console.WriteLine();

            if (this._outputToFile)
                this._output.WriteLine();
        }

        private sealed class TestOutputHelperTraceListener : TraceListener
        {
            private readonly TestOutputWriter _testOutputHelper;

            public TestOutputHelperTraceListener(TestOutputWriter testOutputHelper) => this._testOutputHelper = testOutputHelper;

            public override void Write(string message) { } // Ignore trace category prefix stuff

            public override void WriteLine(string message) => this._testOutputHelper.WriteLine(message);
        }
    }
}