using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal abstract class ContractClassWriter : ArtifactWriterBase
    {
        #region Fields
        private readonly ICollection<SchemaDefinition> _schemas;
        #endregion

        #region Properties
        public override string LayerName => CodeGeneration.LayerName.DomainModel;
        public override string RegionName => "Contracts";
        public abstract JsonSerializerFlavor SerializerFlavor { get; }
        public abstract string DateOnlyJsonConverterNamespace { get; }
        #endregion

        #region Constructor
        protected ContractClassWriter(CodeGenerationModel model, CodeGenerationOutputFilter outputFilter)
        {
            _schemas = model.GetSchemas(outputFilter)
                            .Where(x => x.GetType() == typeof(ObjectSchema) // Equal is important here, since we don't want UserDefinedTypeSchema which inherits from ObjectSchema
                                     || x is EnumSchema)
                            .ToArray();
        }
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => _schemas.Any();

        public override void Write(CodeGenerationContext context)
        {
            var namespaceGroups = _schemas.GroupBy(x => x.AbsoluteNamespace).OrderBy(x => x.Key).ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, SchemaDefinition> namespaceGroup = namespaceGroups[i];
                CSharpStatementScope scope = /*namespaceGroup.Key != null ? */context.CreateOutputScope(namespaceGroup.Key)/* : context.Output*/;
                IList<SchemaDefinition> schemas = namespaceGroup.OrderBy(x => x.DefinitionName).ToArray();
                for (int j = 0; j < schemas.Count; j++)
                {
                    SchemaDefinition schema = schemas[j];
                    switch (schema)
                    {
                        case ObjectSchema objectSchema:
                            ProcessObjectSchema(context, scope, objectSchema);
                            break;

                        case EnumSchema enumSchema:
                            ProcessEnumSchema(context, scope, enumSchema);
                            break;
                    }

                    if (j + 1 < schemas.Count)
                        scope.AddSeparator();
                }

                if (i + 1 < namespaceGroups.Length)
                    context.AddSeparator();
            }
        }
        #endregion

        #region Protected Methods
        protected virtual void BeginProcessClass(ObjectSchema schema, ICollection<CSharpAnnotation> classAnnotations, CodeGenerationContext context) { }

        protected virtual bool ProcessProperty(ObjectSchema schema, ObjectSchemaProperty property, ICollection<CSharpAnnotation> propertyAnnotations, CodeGenerationContext context) => true;

        protected virtual void EndProcessClass(ObjectSchema schema, CSharpClass @class, CodeGenerationContext context) { }

        protected void AddJsonSerializerUsing(CodeGenerationContext context) => AddJsonSerializerUsing(context, SerializerFlavor);
        #endregion

        #region Private Methods
        private void ProcessObjectSchema(CodeGenerationContext context, CSharpStatementScope scope, ObjectSchema schema)
        {
            ICollection<CSharpAnnotation> classAnnotations = new Collection<CSharpAnnotation>();
            BeginProcessClass(schema, classAnnotations, context);

            CSharpClass @class = scope.AddClass(schema.DefinitionName, CSharpModifiers.Public | CSharpModifiers.Sealed, classAnnotations);
            ICollection<string> ctorAssignments = new Collection<string>();
            foreach (ObjectSchemaProperty property in schema.Properties)
            {
                ICollection<CSharpAnnotation> propertyAnnotations = new Collection<CSharpAnnotation>();
                if (!ProcessProperty(schema, property, propertyAnnotations, context))
                    continue;

                CSharpValue defaultValue = null;
                if (property.DefaultValue != null)
                {
                    defaultValue = context.BuildDefaultValueLiteral(property.DefaultValue);
                    context.AddUsing<DefaultValueAttribute>();
                    propertyAnnotations.Add(new CSharpAnnotation("DefaultValue", defaultValue));
                }

                if (property.Type is PrimitiveTypeReference { Type: PrimitiveType.Date })
                {
                    AddJsonSerializerUsing(context);
                    context.AddUsing(DateOnlyJsonConverterNamespace);
                    propertyAnnotations.Add(CollectDateOnlyJsonAnnotation());
                }

                TypeReference propertyType = property.Type;
                string clrTypeName = context.ResolveTypeName(propertyType, enumerableBehavior: EnumerableBehavior.None);
                @class.AddProperty(property.Name, !propertyType.IsEnumerable ? clrTypeName : $"{nameof(IList<object>)}<{clrTypeName}>", propertyAnnotations)
                      .Getter(null)
                      .Setter(null, propertyType.IsEnumerable ? CSharpModifiers.Private : default)
                      .Initializer(defaultValue);

                if (propertyType.IsEnumerable)
                    ctorAssignments.Add($"{property.Name.Value} = new {nameof(List<object>)}<{clrTypeName}>();");
            }

            if (ctorAssignments.Any())
            {
                context.AddUsing<ICollection<object>>()
                       .AddUsing<List<object>>();

                @class.AddSeparator()
                      .AddConstructor(String.Join(Environment.NewLine, ctorAssignments));
            }

            EndProcessClass(schema, @class, context);
        }

        private static void ProcessEnumSchema(CodeGenerationContext context, CSharpStatementScope scope, EnumSchema schema)
        {
            ICollection<CSharpAnnotation> annotations = new Collection<CSharpAnnotation>();
            if (schema.IsFlaggable)
            {
                context.AddUsing<FlagsAttribute>();
                annotations.Add(new CSharpAnnotation("Flags"));
            }

            CSharpEnum @enum = scope.AddEnum(schema.DefinitionName, CSharpModifiers.Public, annotations);
            foreach (EnumSchemaMember member in schema.Members)
            {
                @enum.AddMember(member.Name, member.StringValue)
                     .BaseType(context.ResolveTypeName(schema.BaseType));
            }
        }

        private static void AddJsonSerializerUsing(CodeGenerationContext context, JsonSerializerFlavor flavor)
        {
            string @namespace = GetJsonSerializerNamespace(flavor);
            context.AddUsing(@namespace);
        }

        private CSharpAnnotation CollectDateOnlyJsonAnnotation() => CollectDateOnlyJsonAnnotation(SerializerFlavor);
        private static CSharpAnnotation CollectDateOnlyJsonAnnotation(JsonSerializerFlavor flavor) => flavor switch
        {
            JsonSerializerFlavor.NewtonsoftJson => new CSharpAnnotation("JsonConverter", new CSharpValue("typeof(DateOnlyJsonConverter)")),
            JsonSerializerFlavor.SystemTextJson => new CSharpAnnotation("JsonConverter", new CSharpValue("typeof(DateOnlyJsonConverter)")),
            _ => throw new ArgumentOutOfRangeException(nameof(flavor), flavor, null)
        };

        private static string GetJsonSerializerNamespace(JsonSerializerFlavor flavor) => flavor switch
        {
            JsonSerializerFlavor.NewtonsoftJson => "Newtonsoft.Json",
            JsonSerializerFlavor.SystemTextJson => "System.Text.Json.Serialization",
            _ => throw new ArgumentOutOfRangeException(nameof(flavor), flavor, null)
        };
        #endregion
    }
}