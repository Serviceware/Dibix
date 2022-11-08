namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameterInfo
    {
        public string ParameterName { get; }
        public string FilePath { get; }
        public int Line { get; }
        public int Column { get; }

        public ActionParameterInfo(string parameterName, string filePath, int line, int column)
        {
            this.ParameterName = parameterName;
            this.FilePath = filePath;
            this.Line = line;
            this.Column = column;
        }
    }
}