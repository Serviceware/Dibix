using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumSchema : SchemaDefinition
    {
        public PrimitiveTypeReference BaseType { get; internal set; }
        public bool IsFlaggable { get; internal set; }
        public ICollection<EnumSchemaMember> Members { get; }

        public EnumSchema(string absoluteNamespace, string relativeNamespace, string definitionName, SchemaDefinitionSource source, SourceLocation location) : base(absoluteNamespace, relativeNamespace, definitionName, source, location)
        {
            BaseType = new PrimitiveTypeReference(PrimitiveType.Int32, isNullable: false, isEnumerable: false, size: null, location: location);
            Members = new SortedSet<EnumSchemaMember>(Comparer<EnumSchemaMember>.Create(CompareEnumSchemaMember));
        }

        private static int CompareEnumSchemaMember(EnumSchemaMember x, EnumSchemaMember y)
        {
            IComparable a = x.ActualValue;
            IComparable b = y.ActualValue;
            return a.CompareTo(b);
        }
    }
}