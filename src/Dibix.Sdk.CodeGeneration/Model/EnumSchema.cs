using System;
using System.Collections.Generic;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumSchema : SchemaDefinition
    {
        public PrimitiveTypeReference BaseType { get; internal set; }
        public bool IsFlaggable { get; internal set; }
        public ICollection<EnumSchemaMember> Members { get; }

        public EnumSchema(string @namespace, string definitionName, SchemaDefinitionSource source, SourceLocation location) : base(@namespace, definitionName, source, location)
        {
            BaseType = new PrimitiveTypeReference(PrimitiveType.Int32, isNullable: false, isEnumerable: false, location);
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