using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk
{
    public sealed class ConfigureDebugCommand : ActionCommand
    {
        private readonly Option<string> _httpHostDirectoryOption;
        private readonly Option<string> _workerHostDirectoryOption;
        private readonly Option<bool> _attachDebuggerOption;
        private string _httpHostSourceDirectory;
        private string _workerHostSourceDirectory;
        private string _httpHostTargetDirectory;
        private string _workerHostTargetDirectory;

        public ConfigureDebugCommand() : base("configure", "Configure debugging for Dibix hosts")
        {
            _httpHostDirectoryOption = new Option<string>("--http-host-directory", "h") { Description = "The directory of the http host to mount Extension and Packages folders from." };
            _workerHostDirectoryOption = new Option<string>("--worker-host-directory", "w") { Description = "The directory of the worker host to mount Extension and Workers folders from." };
            _attachDebuggerOption = new Option<bool>("--attach-debugger", "a") { Description = "Attach a debugger to the running Dibix host." };

            Add(_httpHostDirectoryOption);
            Add(_workerHostDirectoryOption);
            Add(_attachDebuggerOption);
        }

        protected override void Validate(CommandResult commandResult)
        {
            CollectSourceDirectories(commandResult);
            CollectTargetDirectories(commandResult);
        }

        protected override Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken)
        {
            AttachIfNecessary(parseResult);

#if NET
            SetupSymlinks();
#else
            WriteLineWarning("Skipped creating symlinks because they are not supported for the net48 version of this CLI");
#endif

            return Task.FromResult(0);
        }

        private void CollectSourceDirectories(CommandResult commandResult)
        {
            static string CollectSourceDirectory(string name, string targetFramework, Action<string> errorReporter)
            {
                string sourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, $"../../../../{name}/bin/Debug/{targetFramework}"));
                if (!Directory.Exists(sourcePath))
                {
                    errorReporter($"Error: Could not resolve binary directory for '{name}'. Tried '{sourcePath}'");
                }
                return sourcePath;
            }

            string targetFramework = new DirectoryInfo(AppContext.BaseDirectory).Name;
            _httpHostSourceDirectory = CollectSourceDirectory("Dibix.Http.Host", targetFramework, commandResult.AddError);
            _workerHostSourceDirectory = CollectSourceDirectory("Dibix.Worker.Host", targetFramework, commandResult.AddError);
        }

        private void CollectTargetDirectories(CommandResult commandResult)
        {
            static string CollectTargetDirectory(Option<string> option, string environmentVariableName, Func<Option<string>, string> valueResolver, Action<string> errorReporter, ref bool loggedMessages)
            {
                string targetDirectory = valueResolver(option);
                if (targetDirectory != null)
                    return targetDirectory;

                // Enable configuring the environment variable via the Windows environment variables settings dialog, but on linux fallback to process, where registry access is not available
                EnvironmentVariableTarget target = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? EnvironmentVariableTarget.User : EnvironmentVariableTarget.Process;
                targetDirectory = Environment.GetEnvironmentVariable(environmentVariableName, target);

                if (targetDirectory != null)
                {
                    WriteLineDebug($"Read {option.Name} option from {environmentVariableName} environment variable because it wasn't passed to this command");
                    loggedMessages = true;
                }
                else
                {
                    //errorReporter($"Error: Either the {option.Name} option or {environmentVariableName} environment variable must be provided.");
                }
                return targetDirectory;
            }

            bool loggedMessages = false;
            _httpHostTargetDirectory = CollectTargetDirectory(_httpHostDirectoryOption, EnvironmentVariableName.HttpHostDirectory, commandResult.GetValue, commandResult.AddError, ref loggedMessages);
            _workerHostTargetDirectory = CollectTargetDirectory(_workerHostDirectoryOption, EnvironmentVariableName.WorkerHostDirectory, commandResult.GetValue, commandResult.AddError, ref loggedMessages);

            if (loggedMessages)
                Console.WriteLine();
        }

        private void AttachIfNecessary(ParseResult parseResult)
        {
            if (!parseResult.GetValue(_attachDebuggerOption))
                return;

            if (Debugger.IsAttached)
            {
                WriteLineWarning("A debugger is already attached.");
            }
            else
            {
                Debugger.Launch();
            }
        }

        private void SetupSymlinks()
        {
            SetupSymlinkDibixHttpHost();
            SetupSymlinkWorkerHttpHost();

            WriteLineSuccess("Debugging configuration complete.");
        }
        private static void SetupSymlinks(string applicationName, string sourceDirectory, string targetDirectory, Option<string> option, string environmentVariableName, IEnumerable<string> directories)
        {
            if (targetDirectory != null)
            {
                Console.WriteLine($"Setting up symlinks for '{applicationName}'..");

                foreach (string directory in directories)
                {
                    string source = Path.Combine(sourceDirectory, directory);
                    string target = Path.Combine(targetDirectory, directory);
                    if (Directory.Exists(source))
                    {
                        WriteLineDebug($"Target directory '{target}' already exists. Removing '{source}'..");
                        Directory.Delete(source, recursive: true);
                    }

                    WriteLineDebug($"Creating symbolic link from '{source}' to '{target}'");
#if NET
                    Directory.CreateSymbolicLink(source, target);
#else
                    throw new NotSupportedException("Creating symbolic links is not supported for the net48 version of this CLI");
#endif
                }
            }
            else
            {
                WriteLineWarning($"Skipping symlink creation for '{applicationName}' because neither {option.Name} nor {environmentVariableName} was set.");
            }

            Console.WriteLine();
        }

        private void SetupSymlinkDibixHttpHost()
        {
            const string applicationName = "Dibix.Http.Host";
            if (_httpHostSourceDirectory == null)
                throw new InvalidOperationException($"Source directory for {applicationName} not initialized");

            string[] directories = ["Extension", "Packages"];
            SetupSymlinks(applicationName, _httpHostSourceDirectory, _httpHostTargetDirectory, _httpHostDirectoryOption, EnvironmentVariableName.HttpHostDirectory, directories);
        }

        private void SetupSymlinkWorkerHttpHost()
        {
            const string applicationName = "Dibix.Worker.Host";
            if (_workerHostSourceDirectory == null)
                throw new InvalidOperationException($"Source directory for {applicationName} not initialized");

            string[] directories = ["Extension", "Workers"];
            SetupSymlinks(applicationName, _workerHostSourceDirectory, _workerHostTargetDirectory, _workerHostDirectoryOption, EnvironmentVariableName.WorkerHostDirectory, directories);
        }

        private static void WriteLineDebug(string message) => WriteLine(message, ConsoleColor.DarkGray);
        private static void WriteLineWarning(string message) => WriteLine(message, ConsoleColor.Yellow);
        private static void WriteLineSuccess(string message) => WriteLine(message, ConsoleColor.Green);
        private static void WriteLine(string message, ConsoleColor foregroundColor)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = foregroundColor;
                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }
}