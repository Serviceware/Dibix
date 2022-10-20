using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class EnumSchemaExtensions
    {
        public static EnumSchemaMember GetEnumMember(this EnumMemberNumericReference reference, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger)
        {
            EnumSchema schema = (EnumSchema)schemaDefinitionResolver.Resolve(reference.Type);
            EnumSchemaMember member = schema.Members.SingleOrDefault(x => Equals(x.ActualValue, reference.Value));
            if (member != null)
                return member;

            logger.LogError($"Enum '{schema.FullName}' does not define a member with value '{reference.Value}'", reference.Source, reference.Line, reference.Column);
            return null;
        }
        public static EnumSchemaMember GetEnumMember(this EnumMemberStringReference reference, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger)
        {
            EnumSchema schema = (EnumSchema)schemaDefinitionResolver.Resolve(reference.Type);
            EnumSchemaMember member = schema.Members.SingleOrDefault(x => x.Name == reference.Value);
            if (member != null)
                return member;

            logger.LogError($"Enum '{schema.FullName}' does not define a member named '{reference.Value}'", reference.Source, reference.Line, reference.Column);
            return null;
        }
    }
}