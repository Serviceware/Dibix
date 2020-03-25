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
            if (args.Length != 2)
                return PrintHelp();

            string runnerName = args[0];
            string inputConfigurationFile = args[1];

            ILogger logger = new ConsoleLogger();
            if (!TaskRunner.Execute(runnerName, inputConfigurationFile, logger))
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