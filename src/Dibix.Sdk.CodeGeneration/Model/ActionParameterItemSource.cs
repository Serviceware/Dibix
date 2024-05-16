namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterItemSource
    {
        public string ParameterName { get; }
        public ActionParameterSource Source { get; }
        public SourceLocation Location { get; }

        public ActionParameterItemSource(string parameterName, ActionParameterSource source, SourceLocation location)
        {
            ParameterName = parameterName;
            Source = source;
            Location = location;
        }
    }
}