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
        private readonly StreamWriter _output;
        private readonly bool _outputToFile;
        private bool _collectingLine;
        private readonly Process _tail;

        public override Encoding Encoding => Encoding.UTF8;

        public TestOutputWriter(TestContext testContext, TestResultComposer testResultComposer, bool outputToFile, bool tailOutput)
        {
            _outputToFile = outputToFile;

            if (!_outputToFile) 
                return;

            string outputPath = testResultComposer.AddTestFile(OutputFileName);
            _output = new StreamWriter(outputPath);

            if (!tailOutput) 
                return;

            _tail = Process.Start(new ProcessStartInfo("powershell", $"-Command Write '{testContext.TestName}'; Get-Content '{outputPath}' -Wait") { UseShellExecute = true });
            if (_tail == null || _tail.HasExited)
                throw new InvalidOperationException("Could not tail output");

            Process.GetCurrentProcess().Exited += (_, _) => EndOutputTail();
            Console.SetOut(this);
        }

        public TraceListener CreateTraceListener() => new TestOutputHelperTraceListener(this);

        public override void Write(string message) => Write(message, appendLine: false);

        public override void WriteLine() => Write(message: String.Empty, appendLine: true);

        public override Task WriteLineAsync()
        {
            Write(message: String.Empty, appendLine: true);
            return Task.CompletedTask;
        }

        public override void WriteLine(string message) => Write(message, appendLine: true);

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (!_outputToFile)
                return;

            _output.Dispose();
            EndOutputTail();
        }

        private void EndOutputTail()
        {
            if (_tail is { HasExited: false })
                _tail.Kill();
        }

        private void Write(string message, bool appendLine)
        {
            string[] lines = message.Split('\n')
                                    .Select(x => x.Trim('\r'))
                                    .ToArray();

            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) 
                    AppendLine();

                string line = lines[i];

                if (!_collectingLine || i > 0)
                    line = $"[{DateTime.Now:O}] {line}";
                
                if (_outputToFile)
                    _output.Write(line);
            }

            if (appendLine)
                AppendLine();

            _collectingLine = !appendLine;

            if (_outputToFile)
                _output.Flush();
        }

        private void AppendLine()
        {
            if (_outputToFile)
                _output.WriteLine();
        }

        private sealed class TestOutputHelperTraceListener : TraceListener
        {
            private readonly TestOutputWriter _testOutputHelper;

            public TestOutputHelperTraceListener(TestOutputWriter testOutputHelper) => _testOutputHelper = testOutputHelper;

            public override void Write(string message) { } // Ignore trace category prefix stuff

            public override void WriteLine(string message) => _testOutputHelper.WriteLine(message);
        }
    }
}