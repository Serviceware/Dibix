using System;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class OutputConfiguration
    {
        public Type Writer { get; set; }
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public CommandTextFormatting? Formatting { get; set; }
    }
}