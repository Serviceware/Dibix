﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
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
        protected abstract bool GenerateRuntimeSpecifics { get; }
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
                                      .GroupBy(x => context.GetRelativeNamespace(this.LayerName, x.Namespace))
                                      .ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, SchemaDefinition> namespaceGroup = namespaceGroups[i];
                CSharpStatementScope scope = namespaceGroup.Key != null ? context.Output.BeginScope(namespaceGroup.Key) : context.Output;
                IList<SchemaDefinition> schemas = namespaceGroup.ToArray();
                for (int j = 0; j < schemas.Count; j++)
                {
                    SchemaDefinition schema = schemas[j];
                    switch (schema)
                    {
                        case ObjectSchema objectSchema:
                            ProcessObjectSchema(context, scope, objectSchema, this.GenerateRuntimeSpecifics);
                            break;

                        case EnumSchema enumSchema:
                            ProcessEnumSchema(context, scope, enumSchema);
                            break;
                    }

                    if (j + 1 < schemas.Count)
                        scope.AddSeparator();
                }

                if (i + 1 < namespaceGroups.Length)
                    context.Output.AddSeparator();
            }
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

        private static void ProcessObjectSchema(CodeGenerationContext context, CSharpStatementScope scope, ObjectSchema schema, bool generateRuntimeSpecifics)
        {
            ICollection<CSharpAnnotation> classAnnotations = new Collection<CSharpAnnotation>();
            if (generateRuntimeSpecifics && !String.IsNullOrEmpty(schema.WcfNamespace)) // Serialization/Compatibility
            {
                context.AddUsing<DataMemberAttribute>();
                classAnnotations.Add(new CSharpAnnotation("DataContract").AddProperty("Namespace", new CSharpStringValue(schema.WcfNamespace)));
            }

            CSharpModifiers classVisibility = context.GeneratePublicArtifacts ? CSharpModifiers.Public : CSharpModifiers.Internal;
            CSharpClass @class = scope.AddClass(schema.DefinitionName, classVisibility | CSharpModifiers.Sealed, classAnnotations);
            ICollection<string> ctorAssignments = new Collection<string>();
            ICollection<string> shouldSerializeMethods = new Collection<string>();
            foreach (ObjectSchemaProperty property in schema.Properties)
            {
                ICollection<CSharpAnnotation> propertyAnnotations = new Collection<CSharpAnnotation>();
                CSharpValue defaultValue = null;
                if (generateRuntimeSpecifics)
                {
                    if (!String.IsNullOrEmpty(schema.WcfNamespace))
                        propertyAnnotations.Add(new CSharpAnnotation("DataMember")); // Serialization/Compatibility

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

                    if (property.DateTimeKind != default)
                    {
                        context.AddUsing("Dibix");
                        context.AddUsing<DateTimeKind>();
                        propertyAnnotations.Add(new CSharpAnnotation("DateTimeKind", new CSharpValue($"DateTimeKind.{property.DateTimeKind}"))); // Dibix runtime
                    }
                }

                if (property.DefaultValue != null)
                {
                    defaultValue = context.BuildDefaultValueLiteral(property.DefaultValue);
                    context.AddUsing<DefaultValueAttribute>();
                    propertyAnnotations.Add(new CSharpAnnotation("DefaultValue", defaultValue));
                }

                switch (property.SerializationBehavior)
                {
                    case SerializationBehavior.Always:
                        break;

                    case SerializationBehavior.IfNotEmpty:
                        if (generateRuntimeSpecifics)
                        {
                            if (!property.Type.IsEnumerable)
                            {
                                AddJsonReference(context);
                                propertyAnnotations.Add(new CSharpAnnotation("JsonProperty").AddProperty("NullValueHandling", new CSharpValue("NullValueHandling.Ignore"))); // Serialization
                            }
                            else
                            {
                                shouldSerializeMethods.Add(property.Name);
                            }
                        }

                        break;

                    case SerializationBehavior.Never:
                        if (generateRuntimeSpecifics)
                        {
                            AddJsonReference(context);
                            propertyAnnotations.Add(new CSharpAnnotation("JsonIgnore")); // Serialization
                        }
                        else
                            continue;

                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(property.SerializationBehavior), property.SerializationBehavior, null);
                }

                if (property.Obfuscated && generateRuntimeSpecifics)
                    propertyAnnotations.Add(new CSharpAnnotation("Obfuscated"));

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

            if (generateRuntimeSpecifics && shouldSerializeMethods.Any())
            {
                context.AddUsing(typeof(Enumerable).Namespace); // Enumerable.Any();

                @class.AddSeparator();

                foreach (string shouldSerializeMethod in shouldSerializeMethods)
                {
                    @class.AddMethod($"ShouldSerialize{shouldSerializeMethod}", "bool", $"return {shouldSerializeMethod}.Any();"); // Serialization
                }
            }
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

        private static void AddJsonReference(CodeGenerationContext context)
        {
            context.AddUsing<JsonPropertyAttribute>();
            context.Model.AdditionalAssemblyReferences.Add(Path.GetFileName(typeof(JsonPropertyAttribute).Assembly.Location));
        }
        #endregion
    }
}