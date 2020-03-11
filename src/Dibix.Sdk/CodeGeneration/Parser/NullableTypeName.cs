using System.Diagnostics;

namespace Dibix.Sdk.CodeGeneration
{
    [DebuggerDisplay("{ToString()}")]
    public sealed class NullableTypeName
    {
        // Used for lookup - basically no trailing ? for nullable
        public string Name { get; }
        public bool IsNullable { get; }

        private NullableTypeName(string input)
        {
            this.IsNullable = input[input.Length - 1] == '?';
            this.Name = input.TrimEnd('?');
        }

        public static implicit operator NullableTypeName(string input) => new NullableTypeName(input);
        
        public static implicit operator string(NullableTypeName reference) => reference.ToString();

        public override string ToString() => $"{this.Name}{(this.IsNullable ? "?" : null)}";
    }
}