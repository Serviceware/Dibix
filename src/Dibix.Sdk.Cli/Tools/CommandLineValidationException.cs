using System;

namespace Dibix.Sdk.Cli.Tools
{
    internal sealed class CommandLineValidationException : Exception
    {
        public CommandLineValidationException(string message) : base(message)
        {
        }
    }
}