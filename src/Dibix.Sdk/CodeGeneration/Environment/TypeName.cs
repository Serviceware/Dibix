using System;
using System.Diagnostics;

namespace Dibix.Sdk.CodeGeneration
{
    [DebuggerDisplay("{ToString()}")]
    public class TypeName
    {
        private readonly string _originalTypeName;
        private readonly bool _isNullable;

        public string AssemblyName { get; }
        public string NormalizedTypeName { get; }
        public bool IsAssemblyQualified { get; }
        public Type ClrType { get; set; }
        public string CSharpTypeName => this.BuildCSharpTypeName();

        private TypeName(string originalTypeName, string assemblyName, string normalizedTypeName, bool isAssemblyQualified, bool isNullable)
        {
            this._originalTypeName = originalTypeName;
            this._isNullable = isNullable;
            this.AssemblyName = assemblyName;
            this.NormalizedTypeName = normalizedTypeName;
            this.IsAssemblyQualified = isAssemblyQualified;
        }

        public static implicit operator string(TypeName typeName) => typeName._originalTypeName;

        public static implicit operator TypeName(string typeName)
        {
            bool isAssemblyQualified = typeName.Contains(",");
            string[] parts = typeName.Split(',');
            string assemblyName = isAssemblyQualified ? parts[1] : null;
            string actualTypeName = parts[0];
            bool isNullable = actualTypeName.EndsWith("?", StringComparison.Ordinal);
            if (isNullable)
                actualTypeName = actualTypeName.TrimEnd('?');

            return new TypeName(typeName, assemblyName, actualTypeName, isAssemblyQualified, isNullable);
        }

        public override string ToString() => this._originalTypeName;

        private string BuildCSharpTypeName()
        {
            string typeName = this.ClrType != null ? this.ClrType.ToCSharpTypeName() : this.NormalizedTypeName;
            return $"{typeName}{(this._isNullable ? "?" : null)}";
        }
    }
}