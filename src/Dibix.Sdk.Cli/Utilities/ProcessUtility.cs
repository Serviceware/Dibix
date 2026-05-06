using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli
{
    internal static class ProcessUtility
    {
        public static async Task Execute(string fileName, string arguments, string workingDirectory = null, IDictionary<string, string> environmentVariables = null)
        {
            using Process process = Start(fileName, arguments, redirectOutput: false, workingDirectory, environmentVariables);
            await WaitForExitAsync(process).ConfigureAwait(false);
            ThrowIfFailed(process, fileName, arguments, standardOutput: null, standardError: null);
        }

        public static async Task<string> Capture(string fileName, string arguments, string workingDirectory = null, IDictionary<string, string> environmentVariables = null)
        {
            using Process process = Start(fileName, arguments, redirectOutput: true, workingDirectory, environmentVariables);

            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

            string standardOutput = (await standardOutputTask.ConfigureAwait(false)).Trim();
            string standardError = (await standardErrorTask.ConfigureAwait(false)).Trim();
            await WaitForExitAsync(process).ConfigureAwait(false);

            ThrowIfFailed(process, fileName, arguments, standardOutput, standardError);
            return standardOutput;
        }

        private static Process Start(string fileName, string arguments, bool redirectOutput, string workingDirectory, IDictionary<string, string> environmentVariables)
        {
            ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectOutput
            };

            if (workingDirectory != null)
                psi.WorkingDirectory = workingDirectory;

            if (environmentVariables != null)
            {
                foreach (KeyValuePair<string, string> environmentVariable in environmentVariables)
                {
                    psi.Environment.Add(environmentVariable);
                }
            }

            Process process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start '{fileName}'");
            return process;
        }

        private static void ThrowIfFailed(Process process, string fileName, string arguments, string standardOutput, string standardError)
        {
            if (process.ExitCode == 0)
                return;

            throw new ProcessExecutionException(fileName, arguments, process.ExitCode, standardOutput, standardError);
        }

        private static Task WaitForExitAsync(Process process)
        {
#if NET
            return process.WaitForExitAsync();
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
    }
}