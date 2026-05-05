using System;

namespace Dibix.Sdk.Cli
{
    internal sealed class CommandLineValidationException : Exception
    {
        public CommandLineValidationException(string message) : base(message)
        {
        }
    }
}