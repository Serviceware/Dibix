namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameterPropertySource : ActionParameterSource
    {
        public string SourceName { get; }
        public string PropertyName { get; }

        internal ActionParameterPropertySource(string sourceName, string propertyName)
        {
            this.SourceName = sourceName;
            this.PropertyName = propertyName;
        }
    }
}