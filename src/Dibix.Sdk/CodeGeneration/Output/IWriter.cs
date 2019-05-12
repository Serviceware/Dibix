using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IWriter
    {
        string Write(string @namespace, string className, CommandTextFormatting formatting, SourceArtifacts artifacts);
    }
}