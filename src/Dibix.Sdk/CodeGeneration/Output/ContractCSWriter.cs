using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Dibix.Sdk.CodeGeneration.CSharp;
using Newtonsoft.Json;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class ContractCSWriter
    {
        #region Public Methods
        public static void Write(DaoCodeGenerationContext context, bool withAnnotations)
        {
            context.AddUsing(typeof(DateTime).Namespace);

            var namespaceGroups = context.Model
                                         .Contracts
                                         .GroupBy(x => context.WriteNamespaces ? NamespaceUtility.BuildRelativeNamespace(context.Model.RootNamespace, LayerName.DomainModel, x.Namespace) : null)
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
                            ProcessObjectSchema(context, scope, objectSchema, withAnnotations);
                            break;

                        case EnumSchema enumSchema:
                            ProcessEnumSchema(scope, enumSchema);
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
        private static void ProcessObjectSchema(DaoCodeGenerationContext context, CSharpStatementScope scope, ObjectSchema schema, bool withAnnotations)
        {
            ICollection<string> classAnnotations = new Collection<string>();
            if (withAnnotations && !String.IsNullOrEmpty(schema.WcfNamespace))
            {
                context.AddUsing(typeof(DataMemberAttribute).Namespace);
                classAnnotations.Add($"DataContract(Namespace = \"{schema.WcfNamespace}\")");
            }

            CSharpClass @class = scope.AddClass(schema.DefinitionName, CSharpModifiers.Public | CSharpModifiers.Sealed, classAnnotations);
            ICollection<string> ctorAssignments = new Collection<string>();
            ICollection<string> shouldSerializeMethods = new Collection<string>();
            foreach (ObjectSchemaProperty property in schema.Properties)
            {
                ICollection<string> propertyAnnotations = new Collection<string>();
                if (withAnnotations)
                {
                    if (!String.IsNullOrEmpty(schema.WcfNamespace))
                        propertyAnnotations.Add("DataMember");

                    if (property.IsPartOfKey)
                    {
                        context.AddUsing(typeof(KeyAttribute).Namespace);
                        context.Model.AdditionalAssemblyReferences.Add("System.ComponentModel.DataAnnotations.dll");
                        propertyAnnotations.Add("Key");
                    }
                    else if (property.IsDiscriminator)
                    {
                        context.AddUsing("Dibix");
                        propertyAnnotations.Add("Discriminator");
                    }
                }

                switch (property.SerializationBehavior)
                {
                    case SerializationBehavior.Always:
                        break;

                    case SerializationBehavior.IfNotEmpty:
                        if (withAnnotations)
                        {
                            if (!property.Type.IsEnumerable)
                            {
                                AddJsonReference(context);
                                propertyAnnotations.Add("JsonProperty(NullValueHandling = NullValueHandling.Ignore)");
                            }
                            else
                            {
                                shouldSerializeMethods.Add(property.Name);
                            }
                        }

                        break;

                    case SerializationBehavior.Never:
                        if (withAnnotations)
                        {
                            AddJsonReference(context);
                            propertyAnnotations.Add("JsonIgnore");
                        }
                        else
                            continue;

                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(property.SerializationBehavior), property.SerializationBehavior, null);
                }

                if (property.Obfuscated) 
                    propertyAnnotations.Add("Obfuscated");

                string clrTypeName = context.ResolveTypeName(property.Type);
                @class.AddProperty(property.Name, !property.Type.IsEnumerable ? clrTypeName : $"ICollection<{clrTypeName}>", propertyAnnotations)
                      .Getter(null)
                      .Setter(null, property.Type.IsEnumerable ? CSharpModifiers.Private : default);

                if (property.Type.IsEnumerable)
                    ctorAssignments.Add($"this.{property.Name} = new Collection<{clrTypeName}>();");
            }

            if (ctorAssignments.Any())
            {
                context.AddUsing(typeof(ICollection<>).Namespace)
                       .AddUsing(typeof(Collection<>).Namespace);

                @class.AddSeparator()
                      .AddConstructor(String.Join(Environment.NewLine, ctorAssignments));
            }

            if (withAnnotations && shouldSerializeMethods.Any())
            {
                context.AddUsing(typeof(Enumerable).Namespace);

                @class.AddSeparator();

                foreach (string shouldSerializeMethod in shouldSerializeMethods)
                {
                    @class.AddMethod($"ShouldSerialize{shouldSerializeMethod}", "bool", $"return {shouldSerializeMethod}.Any();");
                }
            }
        }

        private static void ProcessEnumSchema(CSharpStatementScope scope, EnumSchema schema)
        {
            ICollection<string> annotations = new Collection<string>();
            if (schema.IsFlaggable)
                annotations.Add("Flags");

            CSharpEnum @enum = scope.AddEnum(schema.DefinitionName, CSharpModifiers.Public, annotations);
            foreach (EnumSchemaMember member in schema.Members)
            {
                @enum.AddMember(member.Name, member.Value)
                     .Inherits("int");
            }
        }

        private static void AddJsonReference(DaoCodeGenerationContext context)
        {
            context.AddUsing(typeof(JsonPropertyAttribute).Namespace);
            context.Model.AdditionalAssemblyReferences.Add(Path.GetFileName(typeof(JsonPropertyAttribute).Assembly.Location));
        }
        #endregion
    }
}