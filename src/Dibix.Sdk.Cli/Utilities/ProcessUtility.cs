using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli
{
    internal static class ProcessUtility
    {
        public static async Task Execute(string fileName, string arguments, CancellationToken cancellationToken, string workingDirectory = null, IDictionary<string, string> environmentVariables = null)
        {
            _ = await ExecuteProcess(fileName, arguments, cancellationToken, workingDirectory, environmentVariables, onStandardOutput: ConsoleUtility.WriteLineDebug, onStandardError: ConsoleUtility.WriteLineError).ConfigureAwait(false);
        }

        public static async Task<string> Capture(string fileName, string arguments, CancellationToken cancellationToken, string workingDirectory = null, IDictionary<string, string> environmentVariables = null)
        {
            string standardOutput = await ExecuteProcess(fileName, arguments, cancellationToken, workingDirectory, environmentVariables).ConfigureAwait(false);
            return standardOutput;
        }

        private static async Task<string> ExecuteProcess(string fileName, string arguments, CancellationToken cancellationToken, string workingDirectory = null, IDictionary<string, string> environmentVariables = null, Action<string> onStandardOutput = null, Action<string> onStandardError = null)
        {
            using Process process = StartProcess(fileName, arguments, redirectOutput: true, workingDirectory, environmentVariables);

            ProcessOutputCollector standardOutputCollector = new ProcessOutputCollector(onStandardOutput);
            ProcessOutputCollector standardErrorCollector = new ProcessOutputCollector(onStandardError);

            process.OutputDataReceived += standardOutputCollector.OnDataReceived;
            process.ErrorDataReceived += standardErrorCollector.OnDataReceived;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await WaitForExitAsync(process, cancellationToken).ConfigureAwait(false);

            // Ensures the asynchronous OutputDataReceived/ErrorDataReceived handlers have drained
            // (the async overload guarantees this on net5+, but our net48 polyfill does not).
            // ReSharper disable once MethodHasAsyncOverload
            process.WaitForExit();

            string standardOutput = standardOutputCollector.GetText();
            string standardError = standardErrorCollector.GetText();

            ThrowIfFailed(process, fileName, arguments, standardOutput, standardError);
            return standardOutput;
        }

        private static Process StartProcess(string fileName, string arguments, bool redirectOutput, string workingDirectory, IDictionary<string, string> environmentVariables)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectOutput
            };

            if (workingDirectory != null)
                startInfo.WorkingDirectory = workingDirectory;

            if (environmentVariables != null)
            {
                foreach (KeyValuePair<string, string> environmentVariable in environmentVariables)
                {
                    startInfo.Environment.Add(environmentVariable);
                }
            }

            Process process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start '{fileName}'");
            return process;
        }

        private static void ThrowIfFailed(Process process, string fileName, string arguments, string standardOutput, string standardError)
        {
            if (process.ExitCode == 0)
                return;

            throw new ProcessExecutionException(fileName, arguments, process.ExitCode, standardOutput, standardError);
        }

        private static Task WaitForExitAsync(Process process, CancellationToken cancellationToken)
        {
#if NET
            return process.WaitForExitAsync(cancellationToken);
#else
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => tcs.TrySetResult(true);

            // Guard against the race where the process already exited before we hooked the event
            if (process.HasExited)
                tcs.TrySetResult(true);

            return tcs.Task;
#endif
        }

        private sealed class ProcessOutputCollector
        {
            private readonly StringBuilder _builder = new StringBuilder();
            private readonly Action<string> _onReceived;

            public ProcessOutputCollector(Action<string> onReceived) => _onReceived = onReceived;

            public void OnDataReceived(object sender, DataReceivedEventArgs e)
            {
                if (e.Data == null)
                    return;

                _builder.AppendLine(e.Data);
                _onReceived?.Invoke(e.Data);
            }

            public string GetText() => _builder.ToString().Trim();
        }
    }
}