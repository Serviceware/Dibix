using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli
{
    internal static class ProcessUtility
    {
        public static async Task Execute(string fileName, string arguments, string workingDirectory = null)
        {
            using Process process = Start(fileName, arguments, workingDirectory, redirectOutput: false);
            await WaitForExitAsync(process).ConfigureAwait(false);
            ThrowIfFailed(process, fileName, arguments, standardOutput: null, standardError: null);
        }

        public static async Task<string> Capture(string fileName, string arguments, string workingDirectory = null)
        {
            using Process process = Start(fileName, arguments, workingDirectory, redirectOutput: true);

            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

            string standardOutput = (await standardOutputTask.ConfigureAwait(false)).Trim();
            string standardError = (await standardErrorTask.ConfigureAwait(false)).Trim();
            await WaitForExitAsync(process).ConfigureAwait(false);

            ThrowIfFailed(process, fileName, arguments, standardOutput, standardError);
            return standardOutput;
        }

        private static Process Start(string fileName, string arguments, string workingDirectory, bool redirectOutput)
        {
            ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectOutput,
            };

            if (workingDirectory != null)
                psi.WorkingDirectory = workingDirectory;

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