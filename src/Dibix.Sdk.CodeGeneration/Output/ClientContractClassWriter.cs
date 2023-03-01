using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientContractClassWriter : ContractClassWriter
    {
        public ClientContractClassWriter(CodeGenerationModel model) : base(model, outputFilter: CodeGenerationOutputFilter.Referenced) { }

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