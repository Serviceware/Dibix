namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterItemSource
    {
        public string ParameterName { get; }
        public ActionParameterSource Source { get; }
        public string FilePath { get; }
        public int Line { get; }
        public int Column { get; }

        public ActionParameterItemSource(string parameterName, ActionParameterSource source, string filePath, int line, int column)
        {
            ParameterName = parameterName;
            Source = source;
            FilePath = filePath;
            Line = line;
            Column = column;
        }
    }
}