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
        public static void Write(WriterContext context, bool withAnnotations)
        {
            context.AddUsing(typeof(DateTime).Namespace);

            var namespaceGroups = context.Artifacts
                                         .Contracts
                                         .GroupBy(x => context.Configuration.WriteNamespaces ? x.Namespace.RelativeNamespace : null)
                                         .ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, ContractDefinition> namespaceGroup = namespaceGroups[i];
                CSharpStatementScope scope = namespaceGroup.Key != null ? context.Output.BeginScope(namespaceGroup.Key) : context.Output;
                IList<ContractDefinition> contracts = namespaceGroup.ToArray();
                for (int j = 0; j < contracts.Count; j++)
                {
                    ContractDefinition contract = contracts[j];
                    switch (contract)
                    {
                        case ObjectContract objectContract:
                            ProcessObjectContract(context, scope, objectContract, withAnnotations);
                            break;

                        case EnumContract enumContract:
                            ProcessEnumContract(scope, enumContract);
                            break;
                    }

                    if (j + 1 < contracts.Count)
                        scope.AddSeparator();
                }

                if (i + 1 < namespaceGroups.Length)
                    context.Output.AddSeparator();
            }
        }
        #endregion

        #region Private Methods
        private static void ProcessObjectContract(WriterContext context, CSharpStatementScope scope, ObjectContract contract, bool withAnnotations)
        {
            ICollection<string> classAnnotations = new Collection<string>();
            if (withAnnotations && !String.IsNullOrEmpty(contract.WcfNamespace))
            {
                context.AddUsing(typeof(DataMemberAttribute).Namespace);
                classAnnotations.Add($"DataContract(Namespace = \"{contract.WcfNamespace}\")");
            }

            CSharpClass @class = scope.AddClass(contract.DefinitionName, CSharpModifiers.Public | CSharpModifiers.Sealed, classAnnotations);
            ICollection<string> ctorAssignments = new Collection<string>();
            ICollection<string> shouldSerializeMethods = new Collection<string>();
            foreach (ObjectContractProperty property in contract.Properties)
            {
                ICollection<string> propertyAnnotations = new Collection<string>();
                if (withAnnotations)
                {
                    if (!String.IsNullOrEmpty(contract.WcfNamespace))
                        propertyAnnotations.Add("DataMember");

                    if (property.IsPartOfKey)
                    {
                        context.AddUsing(typeof(KeyAttribute).Namespace);
                        context.Configuration.DetectedReferences.Add("System.ComponentModel.DataAnnotations.dll");
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

                string clrTypeName = ToClrTypeName(property.Type);
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

        private static string ToClrTypeName(ContractPropertyType propertyType)
        {
            string typeName;
            switch (propertyType)
            {
                case PrimitiveContractPropertyType primitiveContractPropertyType: 
                    typeName = ToClrTypeName(primitiveContractPropertyType.Type);
                    break;

                case ContractPropertyTypeReference contractPropertyTypeReference: 
                    typeName = contractPropertyTypeReference.TypeName;
                    break;

                default: throw new ArgumentOutOfRangeException(nameof(propertyType), propertyType, $"Unexpected property type: {propertyType}");
            }

            if (propertyType.IsNullable)
                typeName += '?';

            return typeName;
        }

        private static string ToClrTypeName(ContractPropertyDataType type)
        {
            switch (type)
            {
                case ContractPropertyDataType.Byte:           return "byte";
                case ContractPropertyDataType.Boolean:        return "bool";
                case ContractPropertyDataType.Int16:          return "short";
                case ContractPropertyDataType.Int32:          return "int";
                case ContractPropertyDataType.Int64:          return "long";
                case ContractPropertyDataType.Float:          return "float";
                case ContractPropertyDataType.Double:         return "double";
                case ContractPropertyDataType.Decimal:        return "decimal";
                case ContractPropertyDataType.Binary:         return "byte[]";
                case ContractPropertyDataType.DateTime:       return "DateTime";
                case ContractPropertyDataType.DateTimeOffset: return "DateTimeOffset";
                case ContractPropertyDataType.String:         return "string";
                case ContractPropertyDataType.UUID:           return "Guid";
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static void ProcessEnumContract(CSharpStatementScope scope, EnumContract contract)
        {
            ICollection<string> annotations = new Collection<string>();
            if (contract.IsFlaggable)
                annotations.Add("Flags");

            CSharpEnum @enum = scope.AddEnum(contract.DefinitionName, CSharpModifiers.Public, annotations);
            foreach (EnumContractMember member in contract.Members)
            {
                @enum.AddMember(member.Name, member.Value)
                     .Inherits("int");
            }
        }

        private static void AddJsonReference(WriterContext context)
        {
            context.AddUsing(typeof(JsonPropertyAttribute).Namespace);
            context.Configuration.DetectedReferences.Add(Path.GetFileName(typeof(JsonPropertyAttribute).Assembly.Location));
        }
        #endregion
    }
}