using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientContractClassWriter : ContractClassWriter
    {
        protected override SchemaDefinitionSource SchemaFilter => SchemaDefinitionSource.Local | SchemaDefinitionSource.Generated | SchemaDefinitionSource.Foreign;

        public ClientContractClassWriter(CodeGenerationModel model) : base(model) { }

        protected override bool ProcessProperty(ObjectSchema schema, ObjectSchemaProperty property, ICollection<CSharpAnnotation> propertyAnnotations, CodeGenerationContext context)
        {
            if (property.SerializationBehavior == SerializationBehavior.Never)
                return false;

            if (property.IsRelativeHttpsUrl != default)
            {
                context.AddUsing("Dibix.Http.Client");
                propertyAnnotations.Add(new CSharpAnnotation("RelativeHttpsUrl")); // Dibix runtime
            }

            return true;
        }
    }
}