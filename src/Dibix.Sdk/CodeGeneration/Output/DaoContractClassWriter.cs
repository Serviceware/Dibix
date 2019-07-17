using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoContractClassWriter : IDaoWriter
    {
        #region Properties
        public string RegionName => "Contracts";
        #endregion

        #region IDaoWriter Members
        public bool HasContent(OutputConfiguration configuration, SourceArtifacts artifacts) => artifacts.Contracts.Any();

        public IEnumerable<string> GetGlobalAnnotations(OutputConfiguration configuration) { yield break; }

        public void Write(DaoWriterContext context)
        {
            context.Output.AddUsing("System");

            var namespaceGroups = context.Artifacts
                                         .Contracts
                                         .GroupBy(x => x.Namespace)
                                         .ToArray();

            for (int i = 0; i < namespaceGroups.Length; i++)
            {
                IGrouping<string, ContractDefinition> group = namespaceGroups[i];
                IList<ContractDefinition> contracts = group.ToArray();
                CSharpStatementScope scope = context.Output.BeginScope(group.Key);
                for (int j = 0; j < contracts.Count; j++)
                {
                    ContractDefinition contract = contracts[j];
                    switch (contract)
                    {
                        case ObjectContract objectContract:
                            ProcessObjectContract(context, scope, objectContract);
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
        private static void ProcessObjectContract(DaoWriterContext context, CSharpStatementScope scope, ObjectContract contract)
        {
            ICollection<string> classAnnotations = new Collection<string>();
            if (!String.IsNullOrEmpty(contract.WcfNamespace))
            {
                context.Output.AddUsing("System.Runtime.Serialization");
                classAnnotations.Add($"DataContract(Namespace = \"{contract.WcfNamespace}\")");
            }

            CSharpClass @class = scope.AddClass(contract.DefinitionName, CSharpModifiers.Public | CSharpModifiers.Sealed, classAnnotations);
            ICollection<string> ctorAssignments = new Collection<string>();
            foreach (ObjectContractProperty property in contract.Properties)
            {
                ICollection<string> propertyAnnotations = new Collection<string>();
                if (!String.IsNullOrEmpty(contract.WcfNamespace))
                    propertyAnnotations.Add("DataMember");

                if (property.IsPartOfKey)
                {
                    context.Output.AddUsing("System.ComponentModel.DataAnnotations");
                    context.Configuration.DetectedReferences.Add("System.ComponentModel.DataAnnotations.dll");
                    propertyAnnotations.Add("Key");
                }

                if (property.SkipNull)
                {
                    context.Output.AddUsing("Newtonsoft.Json");
                    context.Configuration.DetectedReferences.Add("Newtonsoft.Json.dll");
                    propertyAnnotations.Add("JsonProperty(NullValueHandling = NullValueHandling.Ignore)");
                }

                @class.AddProperty(property.Name, !property.IsEnumerable ? property.Type : $"ICollection<{property.Type}>", propertyAnnotations)
                      .Getter(null)
                      .Setter(null, property.IsEnumerable ? CSharpModifiers.Private : default);

                if (property.IsEnumerable)
                    ctorAssignments.Add($"this.{property.Name} = new Collection<{property.Type}>();");
            }

            if (!ctorAssignments.Any())
                return;

            context.Output.AddUsing(typeof(ICollection<>).Namespace);
            context.Output.AddUsing(typeof(Collection<>).Namespace);

            @class.AddSeparator()
                  .AddConstructor(String.Join(Environment.NewLine, ctorAssignments));
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
        #endregion
    }
}