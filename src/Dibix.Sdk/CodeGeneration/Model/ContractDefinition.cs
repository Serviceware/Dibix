namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ContractDefinition
    {
        public SchemaDefinition Schema { get; }
        public string FilePath { get; }
        public int Line { get; }
        public int Column { get; }
        public bool IsUsed { get; internal set; }

        public ContractDefinition(SchemaDefinition schema, string filePath, int line, int column)
        {
            this.Schema = schema;
            this.FilePath = filePath;
            this.Line = line;
            this.Column = column;
        }
    }
}