using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoContractClassWriter : ContractClassWriter
    {
        private readonly JsonSerializerFlavor _serializerFlavor;

        public DaoContractClassWriter(CodeGenerationModel model, CodeGenerationOutputFilter outputFilter, JsonSerializerFlavor serializerFlavor) : base(model, outputFilter)
        {
            _serializerFlavor = serializerFlavor;
        }

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

            if (_serializerFlavor == JsonSerializerFlavor.SystemTextJson && property.Type.IsEnumerable)
            {
                AddJsonSerializerReference(context);
                propertyAnnotations.Add(new CSharpAnnotation("JsonInclude"));
            }

            return true;
        }

        protected override void EndProcessClass(ObjectSchema schema, CSharpClass @class, CodeGenerationContext context)
        {
            HandleEmptyCollectionProperties(schema, @class, context, _serializerFlavor);
        }

        private static void HandleEmptyCollectionProperties(ObjectSchema schema, CSharpClass @class, CodeGenerationContext context, JsonSerializerFlavor serializerFlavor)
        {
            if (serializerFlavor != JsonSerializerFlavor.NewtonsoftJson)
                return;

            ICollection<string> properties = schema.Properties
                                                   .Where(x => x.SerializationBehavior == SerializationBehavior.IfNotEmpty && x.Type.IsEnumerable)
                                                   .Select(x => x.Name.Value)
                                                   .ToArray();

            if (!properties.Any())
                return;

            AppendShouldSerializeMethods(schema, @class, context, properties);
        }

        private static void AppendShouldSerializeMethods(ObjectSchema schema, CSharpClass @class, CodeGenerationContext context, IEnumerable<string> properties)
        {
            context.AddUsing(typeof(Enumerable).Namespace); // Enumerable.Any();

            @class.AddSeparator();

            foreach (string property in properties)
            {
                @class.AddMethod($"ShouldSerialize{property}", "bool", $"return {property}.Any();"); // Serialization
            }
        }

        private void HandleSerializationBehavior(ObjectSchemaProperty property, SerializationBehavior serializationBehavior, ICollection<CSharpAnnotation> propertyAnnotations, CodeGenerationContext context)
        {
            switch (serializationBehavior)
            {
                case SerializationBehavior.Always:
                    break;

                case SerializationBehavior.IfNotEmpty:
                    if (property.Type.IsEnumerable)
                    {
                        if (_serializerFlavor == JsonSerializerFlavor.SystemTextJson)
                            propertyAnnotations.Add(new CSharpAnnotation("IgnoreSerializationIfEmptyAttribute"));
                    }
                    else
                    {
                        AddJsonSerializerReference(context);
                        propertyAnnotations.Add(CollectJsonIgnoreNullAnnotation()); // Serialization
                    }
                    break;

                case SerializationBehavior.Never:
                    AddJsonSerializerReference(context);
                    propertyAnnotations.Add(new CSharpAnnotation("JsonIgnore")); // Serialization
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(serializationBehavior), serializationBehavior, null);
            }
        }

        private CSharpAnnotation CollectJsonIgnoreNullAnnotation() => CollectJsonIgnoreNullAnnotation(_serializerFlavor);

        private static CSharpAnnotation CollectJsonIgnoreNullAnnotation(JsonSerializerFlavor flavor) => flavor switch
        {
            JsonSerializerFlavor.NewtonsoftJson => new CSharpAnnotation("JsonProperty").AddProperty("NullValueHandling", new CSharpValue("NullValueHandling.Ignore")),
            JsonSerializerFlavor.SystemTextJson => new CSharpAnnotation("JsonIgnore").AddProperty("Condition", new CSharpValue("JsonIgnoreCondition.WhenWritingNull")),
            _ => throw new ArgumentOutOfRangeException(nameof(flavor), flavor, null)
        };

        private void AddJsonSerializerReference(CodeGenerationContext context) => AddJsonSerializerReference(context, _serializerFlavor);
        private static void AddJsonSerializerReference(CodeGenerationContext context, JsonSerializerFlavor flavor)
        {
            string @namespace = GetJsonSerializerNamespace(flavor);
            context.AddUsing(@namespace);
        }

        private static string GetJsonSerializerNamespace(JsonSerializerFlavor flavor) => flavor switch
        {
            JsonSerializerFlavor.NewtonsoftJson => "Newtonsoft.Json",
            JsonSerializerFlavor.SystemTextJson => "System.Text.Json.Serialization",
            _ => throw new ArgumentOutOfRangeException(nameof(flavor), flavor, null)
        };
    }
}