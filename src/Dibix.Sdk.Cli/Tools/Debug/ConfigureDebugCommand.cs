using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli.Tools
{
    internal sealed class ConfigureDebugCommand : ValidatableActionCommand
    {
        private readonly EnvironmentVariableOption _httpHostDirectoryOption;
        private readonly EnvironmentVariableOption _workerHostDirectoryOption;
        private string _httpHostSourceDirectory;
        private string _workerHostSourceDirectory;
        private string _httpHostTargetDirectory;
        private string _workerHostTargetDirectory;

        public ConfigureDebugCommand() : base("configure", "Configure debugging for Dibix hosts")
        {
            _httpHostDirectoryOption = new EnvironmentVariableOption("--http-host-directory", "h", EnvironmentVariableName.HttpHostDirectory, "The directory of the http host to mount Extension and Packages folders from.");
            _workerHostDirectoryOption = new EnvironmentVariableOption("--worker-host-directory", "w", EnvironmentVariableName.WorkerHostDirectory, "The directory of the worker host to mount Extension and Workers folders from.");

            Add(_httpHostDirectoryOption);
            Add(_workerHostDirectoryOption);
        }

        protected override void Validate(CommandResult commandResult)
        {
            CollectSourceDirectories(commandResult);
            CollectTargetDirectories(commandResult);
        }

        protected override Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken)
        {
#if NET
            SetupSymlinks();
#else
            ConsoleUtility.WriteLineWarning("Skipped creating symlinks because they are not supported for the net48 version of this CLI");
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
            bool loggedMessages = false;
            _httpHostTargetDirectory = _httpHostDirectoryOption.CollectValue(commandResult, isRequired: false, ref loggedMessages);
            _workerHostTargetDirectory = _workerHostDirectoryOption.CollectValue(commandResult, isRequired: false, ref loggedMessages);

            if (loggedMessages)
                Console.WriteLine();
        }

        private void SetupSymlinks()
        {
            SetupSymlinkDibixHttpHost();
            SetupSymlinkWorkerHttpHost();

            ConsoleUtility.WriteLineSuccess("Debugging configuration complete.");
        }
        private static void SetupSymlinks(string applicationName, string sourceDirectory, string targetDirectory, Option<string> option, string environmentVariableName, IEnumerable<string> directories)
        {
            if (targetDirectory != null)
            {
                ConsoleUtility.WriteLineInformation($"Setting up symlinks for '{applicationName}'..");

                foreach (string directory in directories)
                {
                    string source = Path.Combine(sourceDirectory, directory);
                    string target = Path.Combine(targetDirectory, directory);
                    if (Directory.Exists(source))
                    {
                        ConsoleUtility.WriteLineDebug($"Target directory '{target}' already exists. Removing '{source}'..");
                        Directory.Delete(source, recursive: true);
                    }

                    ConsoleUtility.WriteLineDebug($"Creating symbolic link from '{source}' to '{target}'");
#if NET
                    Directory.CreateSymbolicLink(source, target);
#else
                    throw new NotSupportedException("Creating symbolic links is not supported for the net48 version of this CLI");
#endif
                }
            }
            else
            {
                ConsoleUtility.WriteLineWarning($"Skipping symlink creation for '{applicationName}' because neither {option.Name} nor {environmentVariableName} was set.");
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
    }
}