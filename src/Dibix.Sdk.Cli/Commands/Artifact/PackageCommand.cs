using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;

namespace Dibix.Sdk.Cli
{
    internal abstract class PackageCommand : ValidatableActionCommand
    {
        private readonly Argument<string> _packageNameArgument;

        protected string PackageName { get; private set; }
        protected abstract string PackageNameArgumentDescription { get; }

        protected PackageCommand(string name, string description) : base(name, description)
        {
            _packageNameArgument = new Argument<string>("package-name")
            {
                Description = PackageNameArgumentDescription,
                Arity = ArgumentArity.ZeroOrOne
            };

            Add(_packageNameArgument);
        }

        protected abstract void ValidateCore(CommandResult commandResult, ref bool loggedMessages);

        protected sealed override void Validate(CommandResult commandResult)
        {
            bool loggedMessages = false;
            string packageName = commandResult.GetValue(_packageNameArgument);

            if (packageName != null)
            {
                if (!ArtifactUtility.NuGetPackageNames.Contains(packageName))
                {
                    commandResult.AddError($"""
                                            Unknown artifact '{packageName}'.
                                            Possible values are: {String.Join(", ", ArtifactUtility.AllArtifacts.Keys)}
                                            """);
                    loggedMessages = true;
                }
                PackageName = packageName;
            }

            ValidateCore(commandResult, ref loggedMessages);
        }
    }
}