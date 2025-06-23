using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumSchema : SchemaDefinition
    {
        public PrimitiveTypeReference BaseType { get; }
        public bool IsFlaggable { get; }
        public IReadOnlyCollection<EnumSchemaMember> Members { get; }

        public EnumSchema(IEnumerable<EnumSchemaMember> members, string absoluteNamespace, string relativeNamespace, string definitionName, SchemaDefinitionSource source, SourceLocation location, PrimitiveTypeReference baseType = null, bool? isFlaggable = null) : base(absoluteNamespace, relativeNamespace, definitionName, source, location)
        {
            Members = new SortedSet<EnumSchemaMember>(members, Comparer<EnumSchemaMember>.Create(CompareEnumSchemaMember));
            if (!Members.Any())
                throw new ArgumentException("Members collection must not be empty", nameof(members));

            BaseType = baseType ?? CreateDefaultBaseType(location);
            IsFlaggable = isFlaggable ?? DetectIsFlaggable(Members);
        }

        private static PrimitiveTypeReference CreateDefaultBaseType(SourceLocation location) => new PrimitiveTypeReference(PrimitiveType.Int32, isNullable: false, isEnumerable: false, size: null, location: location);

        private static bool DetectIsFlaggable(IEnumerable<EnumSchemaMember> members)
        {
            foreach (EnumSchemaMember member in members)
            {
                if (member.UsesMemberReference)
                    continue;

                if ((member.ActualValue & (member.ActualValue - 1)) != 0)
                    return false;
            }
            return true;
        }

        private static int CompareEnumSchemaMember(EnumSchemaMember x, EnumSchemaMember y)
        {
            IComparable a = x.ActualValue;
            IComparable b = y.ActualValue;
            return a.CompareTo(b);
        }
    }
}