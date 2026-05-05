using System;
using System.CommandLine;
using System.Diagnostics;
using System.Threading.Tasks;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.Cli
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            ILogger logger = new Logger(Console.Out, distinctErrorLogging: true);

            RootCommand root = new DibixRootCommand(logger);
            Option<bool> attachDebuggerOption = new Option<bool>("--attach", "-a") { Description = "Attach a debugger to the running CLI." };
            root.Add(attachDebuggerOption);
            root.Add(new SetConfigurationVariableCommand());
            root.Add(new DebugCommand());
            root.Add(new ArtifactCommand());

            ParseResult parseResult = root.Parse(args);
            AttachDebuggerIfNecessary(parseResult, attachDebuggerOption);

            int exitCode = await parseResult.InvokeAsync().ConfigureAwait(false);
            return exitCode;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.Error.WriteLine($"Unhandled Exception: {e.ExceptionObject}");
            int exitCode = e.ExceptionObject is Exception ex ? ex.HResult : 1;
            Environment.Exit(exitCode);
        }

        private static void AttachDebuggerIfNecessary(ParseResult parseResult, Option<bool> attachDebuggerOption)
        {
            if (!parseResult.GetValue(attachDebuggerOption))
                return;

            if (Debugger.IsAttached)
            {
                ConsoleUtility.WriteLineWarning("A debugger is already attached.");
            }
            else
            {
                Debugger.Launch();
            }
        }
    }
}