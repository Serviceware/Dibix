using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoContractClassWriter : ContractClassWriter
    {
        protected override SchemaDefinitionSource SchemaFilter => SchemaDefinitionSource.Local | SchemaDefinitionSource.Generated;

        public DaoContractClassWriter(CodeGenerationModel model) : base(model) { }

        protected override void BeginProcessClass(ObjectSchema schema, ICollection<CSharpAnnotation> classAnnotations, CodeGenerationContext context)
        {
            // Serialization/Compatibility
            if (String.IsNullOrEmpty(schema.WcfNamespace)) 
                return;

            context.AddUsing<DataMemberAttribute>();
            classAnnotations.Add(new CSharpAnnotation("DataContract").AddProperty("Namespace", new CSharpStringValue(schema.WcfNamespace)));
        }

        protected override bool ProcessProperty(ObjectSchema schema, ObjectSchemaProperty property, ICollection<CSharpAnnotation> propertyAnnotations, CodeGenerationContext context)
        {
            if (!String.IsNullOrEmpty(schema.WcfNamespace))
                propertyAnnotations.Add(new CSharpAnnotation("DataMember")); // Serialization/Compatibility

            if (property.DateTimeKind != default)
            {
                context.AddUsing("Dibix");
                context.AddUsing<DateTimeKind>();
                propertyAnnotations.Add(new CSharpAnnotation("DateTimeKind", new CSharpValue($"DateTimeKind.{property.DateTimeKind}"))); // Dibix runtime
            }

            HandleSerializationBehavior(property, property.SerializationBehavior, propertyAnnotations, context);

            if (property.IsPartOfKey)
            {
                context.AddUsing("System.ComponentModel.DataAnnotations");
                context.Model.AdditionalAssemblyReferences.Add("System.ComponentModel.DataAnnotations.dll");
                propertyAnnotations.Add(new CSharpAnnotation("Key")); // Dibix runtime
            }

            if (property.IsOptional)
            {
                context.AddUsing("Dibix");
                propertyAnnotations.Add(new CSharpAnnotation("Optional")); // OpenAPI description
            }

            if (property.IsDiscriminator)
            {
                context.AddUsing("Dibix");
                propertyAnnotations.Add(new CSharpAnnotation("Discriminator")); // Dibix runtime
            }

            if (property.IsObfuscated)
                propertyAnnotations.Add(new CSharpAnnotation("Obfuscated"));

            return true;
        }

        protected override void EndProcessClass(ObjectSchema schema, CSharpClass @class, CodeGenerationContext context)
        {
            ICollection<string> shouldSerializeMethods = schema.Properties
                                                               .Where(x => x.SerializationBehavior == SerializationBehavior.IfNotEmpty && x.Type.IsEnumerable)
                                                               .Select(x => x.Name)
                                                               .ToArray();
            if (!shouldSerializeMethods.Any()) 
                return;

            context.AddUsing(typeof(Enumerable).Namespace); // Enumerable.Any();

            @class.AddSeparator();

            foreach (string shouldSerializeMethod in shouldSerializeMethods)
            {
                @class.AddMethod($"ShouldSerialize{shouldSerializeMethod}", "bool", $"return {shouldSerializeMethod}.Any();"); // Serialization
            }
        }

        private static void HandleSerializationBehavior(ObjectSchemaProperty property, SerializationBehavior serializationBehavior, ICollection<CSharpAnnotation> propertyAnnotations, CodeGenerationContext context)
        {
            switch (serializationBehavior)
            {
                case SerializationBehavior.Always:
                    break;

                case SerializationBehavior.IfNotEmpty:
                    if (!property.Type.IsEnumerable)
                    {
                        AddJsonReference(context);
                        propertyAnnotations.Add(new CSharpAnnotation("JsonProperty").AddProperty("NullValueHandling", new CSharpValue("NullValueHandling.Ignore"))); // Serialization
                    }
                    break;

                case SerializationBehavior.Never:
                    AddJsonReference(context);
                    propertyAnnotations.Add(new CSharpAnnotation("JsonIgnore")); // Serialization
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(serializationBehavior), serializationBehavior, null);
            }
        }
    }
}