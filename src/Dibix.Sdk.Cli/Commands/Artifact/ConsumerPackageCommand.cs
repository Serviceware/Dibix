using System;
using System.CommandLine.Parsing;

namespace Dibix.Sdk.Cli
{
    internal abstract class ConsumerPackageCommand : PackageCommand
    {
        private readonly EnvironmentVariableOption _consumerDirectoryOption;

        protected ConsumerPackageManager ConsumerPackageManager { get; private set; }

        protected ConsumerPackageCommand(string name, string description, EnvironmentVariableOption consumerDirectoryOption) : base(name, description)
        {
            _consumerDirectoryOption = consumerDirectoryOption;
        }

        protected sealed override void ValidateCore(CommandResult commandResult, ref bool loggedMessages)
        {
            string consumerDirectory = _consumerDirectoryOption.CollectValue(commandResult, isRequired: true, ref loggedMessages);

            if (consumerDirectory != null)
                ConsumerPackageManager = ArtifactUtility.GetPackageManagerForConsumer(consumerDirectory);

            if (loggedMessages)
                Console.WriteLine();
        }
    }
}