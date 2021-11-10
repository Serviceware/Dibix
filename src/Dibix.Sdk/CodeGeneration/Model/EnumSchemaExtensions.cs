using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class EnumSchemaExtensions
    {
        public static bool TryGetEnumMember(this EnumSchema enumSchema, object value, string source, int line, int column, ILogger logger, out EnumSchemaMember enumMember)
        {
            EnumSchemaMember member = enumSchema.Members.SingleOrDefault(x => Equals(x.ActualValue, value));
            if (member != null)
            {
                enumMember = member;
                return true;
            }

            logger.LogError(code: null, $"Enum '{enumSchema.FullName}' does not define a member with value '{value}'", source, line, column);
            enumMember = null;
            return false;
        }
    }
}