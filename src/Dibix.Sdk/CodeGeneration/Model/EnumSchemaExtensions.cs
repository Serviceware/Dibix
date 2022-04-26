using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class EnumSchemaExtensions
    {
        public static EnumSchemaMember GetEnumMember(this EnumMemberNumericReference reference, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            EnumSchema schema = (EnumSchema)schemaRegistry.GetSchema(reference.Type);
            EnumSchemaMember member = schema.Members.SingleOrDefault(x => Equals(x.ActualValue, reference.Value));
            if (member != null)
                return member;

            logger.LogError($"Enum '{schema.FullName}' does not define a member with value '{reference.Value}'", reference.Source, reference.Line, reference.Column);
            return null;
        }
        public static EnumSchemaMember GetEnumMember(this EnumMemberStringReference reference, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            EnumSchema schema = (EnumSchema)schemaRegistry.GetSchema(reference.Type);
            EnumSchemaMember member = schema.Members.SingleOrDefault(x => x.Name == reference.Value);
            if (member != null)
                return member;

            logger.LogError($"Enum '{schema.FullName}' does not define a member named '{reference.Value}'", reference.Source, reference.Line, reference.Column);
            return null;
        }
    }
}