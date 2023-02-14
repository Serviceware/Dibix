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
    }
}