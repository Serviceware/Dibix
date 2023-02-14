using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameterInfo
    {
        public string ParameterName { get; }
        public SourceLocation Location { get; }

        public ActionParameterInfo(string parameterName, SourceLocation location)
        {
            ParameterName = parameterName;
            Location = location;
        }
    }
}