namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameter
    {
        public string ApiParameterName { get; }
        public string InternalParameterName { get; }
        public TypeReference Type { get; }
        public ActionParameterLocation Location { get; }
        public ValueReference DefaultValue { get; }
        public ActionParameterSource Source { get; }
        public bool IsRequired { get; }
        public string FilePath { get; }
        public int Line { get; }
        public int Column { get; }

        public ActionParameter(string apiParameterName, string internalParameterName, TypeReference type, ActionParameterLocation location, bool isRequired, ValueReference defaultValue, ActionParameterSource source, string filePath, int line, int column)
        {
            this.ApiParameterName = apiParameterName;
            this.InternalParameterName = internalParameterName;
            this.Type = type;
            this.Location = location;
            this.IsRequired = isRequired;
            this.DefaultValue = defaultValue;
            this.Source = source;
            this.FilePath = filePath;
            this.Line = line;
            this.Column = column;
        }
    }
}