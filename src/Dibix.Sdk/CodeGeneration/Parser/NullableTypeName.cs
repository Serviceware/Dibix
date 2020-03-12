using System.Diagnostics;

namespace Dibix.Sdk.CodeGeneration
{
    [DebuggerDisplay("{ToString()}")]
    public sealed class NullableTypeName
    {
        public string Name { get; }
        public bool IsNullable { get; }

        private NullableTypeName(string input)
        {
            this.IsNullable = input[input.Length - 1] == '?';
            this.Name = input.TrimEnd('?');
        }

        public static implicit operator NullableTypeName(string input) => new NullableTypeName(input);
        
        public override string ToString() => $"{this.Name}{(this.IsNullable ? "?" : null)}";
    }
}