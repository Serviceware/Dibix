using System;
using System.CommandLine;
using System.Threading.Tasks;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.Cli
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            ILogger logger = new Logger(Console.Out, distinctErrorLogging: true);
            RootCommand root = new DibixRootCommand(logger, "Execute a Dibix SDK command.");
            ParseResult parseResult = root.Parse(args);
            int exitCode = await parseResult.InvokeAsync().ConfigureAwait(false);
            return exitCode;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.Error.WriteLine($"Unhandled Exception: {e.ExceptionObject}");
            int exitCode = e.ExceptionObject is Exception ex ? ex.HResult : 1;
            Environment.Exit(exitCode);
        }
    }
}