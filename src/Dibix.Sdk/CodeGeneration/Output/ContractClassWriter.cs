using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeGeneration.CSharp;
using Newtonsoft.Json;

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
        protected abstract SchemaDefinitionSource SchemaFilter { get; }
        #endregion

        #region Constructor
        protected ContractClassWriter(CodeGenerationModel model)
        {
            this._schemas = model.Schemas.Where(IsValidSchema).ToArray();
        }
        #endregion

        #region Overrides
        public override bool HasContent(CodeGenerationModel model) => this._schemas.Any();

        public override void Write(CodeGenerationContext context)
        {
            var namespaceGroups = this._schemas
                                      .GroupBy(x => x.Namespace)
                                      .ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, SchemaDefinition> namespaceGroup = namespaceGroups[i];
                CSharpStatementScope scope = /*namespaceGroup.Key != null ? */context.CreateOutputScope(namespaceGroup.Key)/* : context.Output*/;
                IList<SchemaDefinition> schemas = namespaceGroup.ToArray();
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

        protected static void AddJsonReference(CodeGenerationContext context)
        {
            context.AddUsing<JsonPropertyAttribute>();
            context.Model.AdditionalAssemblyReferences.Add(Path.GetFileName(typeof(JsonPropertyAttribute).Assembly.Location));
        }
        #endregion

        #region Private Methods
        private bool IsValidSchema(SchemaDefinition schema)
        {
            if (!this.SchemaFilter.HasFlag(schema.Source))
                return false;

            if (schema.GetType() == typeof(ObjectSchema) || schema is EnumSchema)
                return true;

            return false;
        }

        private void ProcessObjectSchema(CodeGenerationContext context, CSharpStatementScope scope, ObjectSchema schema)
        {
            ICollection<CSharpAnnotation> classAnnotations = new Collection<CSharpAnnotation>();
            this.BeginProcessClass(schema, classAnnotations, context);

            CSharpClass @class = scope.AddClass(schema.DefinitionName, CSharpModifiers.Public | CSharpModifiers.Sealed, classAnnotations);
            ICollection<string> ctorAssignments = new Collection<string>();
            foreach (ObjectSchemaProperty property in schema.Properties)
            {
                ICollection<CSharpAnnotation> propertyAnnotations = new Collection<CSharpAnnotation>();
                if (!this.ProcessProperty(schema, property, propertyAnnotations, context))
                    continue;

                CSharpValue defaultValue = null;
                if (property.DefaultValue != null)
                {
                    defaultValue = context.BuildDefaultValueLiteral(property.DefaultValue);
                    context.AddUsing<DefaultValueAttribute>();
                    propertyAnnotations.Add(new CSharpAnnotation("DefaultValue", defaultValue));
                }

                string clrTypeName = context.ResolveTypeName(property.Type, context, includeEnumerable: false);
                @class.AddProperty(property.Name, !property.Type.IsEnumerable ? clrTypeName : $"{nameof(IList<object>)}<{clrTypeName}>", propertyAnnotations)
                      .Getter(null)
                      .Setter(null, property.Type.IsEnumerable ? CSharpModifiers.Private : default)
                      .Initializer(defaultValue);

                if (property.Type.IsEnumerable)
                    ctorAssignments.Add($"this.{property.Name} = new {nameof(Collection<object>)}<{clrTypeName}>();");
            }

            if (ctorAssignments.Any())
            {
                context.AddUsing<ICollection<object>>()
                       .AddUsing<Collection<object>>();

                @class.AddSeparator()
                      .AddConstructor(String.Join(Environment.NewLine, ctorAssignments));
            }

            this.EndProcessClass(schema, @class, context);
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
                     .Inherits("int");
            }
        }
        #endregion
    }
}