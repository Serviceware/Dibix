using System;

namespace Dibix.Sdk.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Console.Error.WriteLine($"Unhandled Exception: {e.ExceptionObject}");
                int exitCode = e.ExceptionObject is Exception ex ? ex.HResult : 1;
                Environment.Exit(exitCode);
            };

            if (args.Length < 1)
                return PrintHelp();

            string runnerName = args[0];

            ILogger logger = new ConsoleLogger();
            if (!TaskRunner.Execute(runnerName, args, logger))
                return PrintHelp();

            return 0;
        }

        private static int PrintHelp()
        {
            Console.WriteLine($"Usage: dibix <{String.Join("|", TaskRunner.RegisteredTaskRunnerNames)}> <inputconfigurationfile>");
            return -1;
        }
    }
}