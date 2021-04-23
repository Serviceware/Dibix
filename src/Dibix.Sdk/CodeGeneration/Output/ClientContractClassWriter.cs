using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientContractClassWriter : ContractClassWriter
    {
        protected override bool GenerateRuntimeSpecifics => false;

        protected override IEnumerable<SchemaDefinition> CollectContracts(CodeGenerationContext context) => base.CollectContracts(context).Union(CollectEndpointSchemas(context));

        private static IEnumerable<SchemaDefinition> CollectEndpointSchemas(CodeGenerationContext context) => CollectEndpointTypeReferences(context).SelectMany(x => CollectEndpointSchemas(x, context));

        private static IEnumerable<SchemaDefinition> CollectEndpointSchemas(TypeReference typeReference, CodeGenerationContext context)
        {
            if (!(typeReference is SchemaTypeReference schemaTypeReference)) 
                yield break;

            SchemaDefinition schema = context.GetSchema(schemaTypeReference);
            yield return schema;

            if (!(schema is ObjectSchema objectSchema)) 
                yield break;

            foreach (SchemaDefinition propertySchema in objectSchema.Properties.SelectMany(x => CollectEndpointSchemas(x.Type, context)))
                yield return propertySchema;
        }
        private static IEnumerable<TypeReference> CollectEndpointTypeReferences(CodeGenerationContext context)
        {
            foreach (ActionDefinition action in context.Model.Controllers.SelectMany(x => x.Actions))
            {
                if (action.RequestBody != null)
                    yield return action.RequestBody.Contract;

                foreach (ActionParameter parameter in action.Parameters)
                    yield return parameter.Type;

                if (action.DefaultResponseType != null)
                    yield return action.DefaultResponseType;
            }
        }
    }
}