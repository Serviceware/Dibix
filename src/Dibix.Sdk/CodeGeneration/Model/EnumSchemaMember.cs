using Newtonsoft.Json;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumSchemaMember
    {
        public string Name { get; }
        public int ActualValue { get; }
        public string StringValue { get; }
        [JsonIgnore]
        public EnumSchema Enum { get; }

        public EnumSchemaMember(string name, int actualValue, string stringValue, EnumSchema @enum)
        {
            this.Name = name;
            this.ActualValue = actualValue;
            this.StringValue = stringValue;
            this.Enum = @enum;
        }
    }
}