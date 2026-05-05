using System;
using System.CommandLine.Parsing;

namespace Dibix.Sdk.Cli
{
    internal abstract class ConsumerPackageCommand : ValidatableActionCommand
    {
        private readonly EnvironmentVariableOption _consumerDirectoryOption;

        protected ConsumerPackageManager ConsumerPackageManager { get; private set; }

        protected ConsumerPackageCommand(string name, string description, EnvironmentVariableOption consumerDirectoryOption) : base(name, description)
        {
            _consumerDirectoryOption = consumerDirectoryOption;
        }

        protected override void Validate(CommandResult commandResult)
        {
            bool loggedMessages = false;
            string consumerDirectory = _consumerDirectoryOption.CollectValue(commandResult, isRequired: true, ref loggedMessages);

            if (consumerDirectory != null)
                ConsumerPackageManager = PackageUtility.GetPackageManagerForConsumer(consumerDirectory);

            if (loggedMessages)
                Console.WriteLine();
        }
    }
}