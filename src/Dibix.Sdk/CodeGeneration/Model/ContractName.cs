using System.Diagnostics;

namespace Dibix.Sdk.CodeGeneration
{
    [DebuggerDisplay("{ToString()}")]
    public sealed class ContractName
    {
        private readonly bool _isNullable;

        // The original user input version
        public string Input { get; }
        
        // Used for lookup - basically no trailing ? for nullable
        public string TypeName { get; set; }

        public ContractName(string input) : this(input, input) { }
        public ContractName(string input, string typeName)
        {
            this._isNullable = typeName[typeName.Length - 1] == '?';
            this.Input = input;
            this.TypeName = typeName.TrimEnd('?');
        }

        public override string ToString() => $"{this.TypeName}{(this._isNullable ? "?" : null)}";
    }
}