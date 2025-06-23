using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class SchemaExtensions
    {
        public static bool IsGridResult(this SqlStatementDefinition sqlStatementDefinition) => sqlStatementDefinition.Results.Any(x => x.Name != null);

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