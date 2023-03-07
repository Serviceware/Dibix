using System;
using System.Collections.Generic;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumSchema : SchemaDefinition
    {
        public bool IsFlaggable { get; internal set; }
        public ICollection<EnumSchemaMember> Members { get; }

        public EnumSchema(string @namespace, string definitionName, SchemaDefinitionSource source, SourceLocation location, bool isFlaggable) : base(@namespace, definitionName, source, location)
        {
            IsFlaggable = isFlaggable;
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