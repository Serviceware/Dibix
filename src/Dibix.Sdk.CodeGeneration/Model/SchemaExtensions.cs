using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class SchemaExtensions
    {
        public static bool IsGridResult(this SqlStatementDefinition sqlStatementDefinition) => sqlStatementDefinition.Results.Any(x => x.Name != null);

        public static EnumSchemaMember GetEnumMember(this EnumMemberNumericReference reference, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            EnumSchema schema = schemaRegistry.GetSchema<EnumSchema>(reference.Type);
            EnumSchemaMember member = schema.Members.SingleOrDefault(x => Equals(x.ActualValue, reference.Value));
            if (member != null)
                return member;

            logger.LogError($"Enum '{schema.FullName}' does not define a member with value '{reference.Value}'", reference.Location.Source, reference.Location.Line, reference.Location.Column);
            return null;
        }
        public static EnumSchemaMember GetEnumMember(this EnumMemberStringReference reference, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            EnumSchema schema = schemaRegistry.GetSchema<EnumSchema>(reference.Type);
            EnumSchemaMember member = schema.Members.SingleOrDefault(x => x.Name == reference.Value);
            if (member != null)
                return member;

            logger.LogError($"Enum '{schema.FullName}' does not define a member named '{reference.Value}'", reference.Location.Source, reference.Location.Line, reference.Location.Column);
            return null;
        }

        public static IEnumerable<SchemaDefinition> GetSchemas(this CodeGenerationModel model, CodeGenerationOutputFilter filter)
        {
            bool MatchesOutputFilter(SchemaDefinition schema)
            {
                switch (filter)
                {
                    case CodeGenerationOutputFilter.Local:
                        return SchemaDefinitionSource.Local.HasFlag(schema.Source);

                    case CodeGenerationOutputFilter.Referenced:
                        return (SchemaDefinitionSource.Local | SchemaDefinitionSource.Foreign).HasFlag(schema.Source) && schema.ReferenceCount > 0;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(filter), filter, null);
                }
            }

            IEnumerable<SchemaDefinition> schemas = model.Schemas.Where(MatchesOutputFilter);
            return schemas;
        }
    }
}