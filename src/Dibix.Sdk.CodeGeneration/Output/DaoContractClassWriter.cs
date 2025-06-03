using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoContractClassWriter : ContractClassWriter
    {
        public override JsonSerializerFlavor SerializerFlavor { get; }
        public override string DateOnlyJsonConverterNamespace { get; }

        public DaoContractClassWriter(CodeGenerationModel model, CodeGenerationOutputFilter outputFilter, ActionCompatibilityLevel compatibilityLevel, JsonSerializerFlavor serializerFlavor) : base(model, outputFilter)
        {
            SerializerFlavor = serializerFlavor;
            DateOnlyJsonConverterNamespace = GetDateOnlyJsonConverterNamespace(compatibilityLevel);
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
                context.AddUsing("Dibix");
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

            if (SerializerFlavor == JsonSerializerFlavor.SystemTextJson && property.Type.IsEnumerable)
            {
                AddJsonSerializerUsing(context);
                propertyAnnotations.Add(new CSharpAnnotation("JsonInclude"));
            }

            return true;
        }

        protected override void EndProcessClass(ObjectSchema schema, CSharpClass @class, CodeGenerationContext context)
        {
            HandleEmptyCollectionProperties(schema, @class, context, SerializerFlavor);
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

            AppendShouldSerializeMethods(@class, context, properties);
        }

        private static void AppendShouldSerializeMethods(CSharpClass @class, CodeGenerationContext context, IEnumerable<string> properties)
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
                        if (SerializerFlavor == JsonSerializerFlavor.SystemTextJson)
                            propertyAnnotations.Add(new CSharpAnnotation("IgnoreSerializationIfEmptyAttribute"));
                    }
                    else
                    {
                        AddJsonSerializerUsing(context);
                        propertyAnnotations.Add(CollectJsonIgnoreAnnotation(property.Type.IsNullable)); // Serialization
                    }
                    break;

                case SerializationBehavior.Never:
                    AddJsonSerializerUsing(context);
                    propertyAnnotations.Add(new CSharpAnnotation("JsonIgnore")); // Serialization
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(serializationBehavior), serializationBehavior, null);
            }
        }

        private CSharpAnnotation CollectJsonIgnoreAnnotation(bool isNullable) => CollectJsonIgnoreAnnotation(SerializerFlavor, isNullable);
        private static CSharpAnnotation CollectJsonIgnoreAnnotation(JsonSerializerFlavor flavor, bool isNullable) => flavor switch
        {
            JsonSerializerFlavor.NewtonsoftJson => new CSharpAnnotation("JsonProperty").AddProperty(isNullable ? "NullValueHandling" : "DefaultValueHandling", new CSharpValue($"{(isNullable ? "NullValueHandling" : "DefaultValueHandling")}.Ignore")),
            JsonSerializerFlavor.SystemTextJson => new CSharpAnnotation("JsonIgnore").AddProperty("Condition", new CSharpValue($"JsonIgnoreCondition.{(isNullable ? "WhenWritingNull" : "WhenWritingDefault")}")),
            _ => throw new ArgumentOutOfRangeException(nameof(flavor), flavor, null)
        };

        private static string GetDateOnlyJsonConverterNamespace(ActionCompatibilityLevel compatibilityLevel) => compatibilityLevel switch
        {
            ActionCompatibilityLevel.Native => "Dibix.Http.Server.AspNetCore",
            ActionCompatibilityLevel.Reflection => "Dibix.Http", // Shared with Dibix.Http.Client
            _ => throw new ArgumentOutOfRangeException(nameof(compatibilityLevel), compatibilityLevel, null)
        };
    }
}