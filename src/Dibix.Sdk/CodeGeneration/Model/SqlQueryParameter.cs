namespace Dibix.Sdk.CodeGeneration
{
    public class SqlQueryParameter
    {
        public string Name { get; set; }
        public TypeReference Type { get; set; }
        public ContractCheck Check { get; set; }
        public DefaultValue DefaultValue { get; set; }
        public bool IsOutput { get; set; }
        public bool Obfuscate { get; set; }
    }
}