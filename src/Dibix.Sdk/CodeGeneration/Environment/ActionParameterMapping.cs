namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameterMapping
    {
        public string SourceName { get; }
        public string SourcePropertyName { get; }
        public string TargetParameterName { get; }

        public ActionParameterMapping(string sourceName, string sourcePropertyName, string targetParameterName)
        {
            this.SourceName = sourceName;
            this.SourcePropertyName = sourcePropertyName;
            this.TargetParameterName = targetParameterName;
        }
    }
}