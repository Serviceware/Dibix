namespace Dibix.Sdk.CodeGeneration
{
    public class SqlQueryParameter
    {
        public string Name { get; set; }
        public TypeReference Type { get; set; }
        public string UdtTypeName { get; set; }
        public ContractCheck Check { get; set; }
        public bool HasDefaultValue { get; set; }
        public object DefaultValue { get; set; }
        public bool Obfuscate { get; set; }
    }
}