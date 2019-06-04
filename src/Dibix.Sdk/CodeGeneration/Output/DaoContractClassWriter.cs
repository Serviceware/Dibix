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
        public bool HasContent(SourceArtifacts artifacts) => artifacts.Contracts.Any();

        public void Write(DaoWriterContext context)
        {
            context.Output.AddUsing("System");

            foreach (IGrouping<string, ContractDefinition> group in context.Artifacts.Contracts.GroupBy(x => x.Namespace))
            {
                string @namespace = group.Key;
                CSharpStatementScope scope = context.Output.BeginScope(@namespace);
                foreach (ContractDefinition contract in group)
                {
                    switch (contract)
                    {
                        case ObjectContract objectContract:
                            ProcessObjectContract(context, scope, objectContract);
                            break;

                        case EnumContract enumContract:
                            ProcessEnumContract(scope, enumContract);
                            break;
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        private static void ProcessObjectContract(DaoWriterContext context, CSharpStatementScope scope, ObjectContract contract)
        {
            CSharpClass @class = scope.AddClass(contract.DefinitionName, CSharpModifiers.Public | CSharpModifiers.Sealed);
            ICollection<string> ctorAssignments = new Collection<string>();
            foreach (ObjectContractProperty property in contract.Properties)
            {
                bool isEnumerable = TryGetArrayType(property.Type, out string arrayType);
                @class.AddProperty(property.Name, !isEnumerable ? property.Type : $"ICollection<{arrayType}>")
                      .Getter(null)
                      .Setter(null, isEnumerable ? CSharpModifiers.Private : default);

                if (isEnumerable)
                    ctorAssignments.Add($"this.{property.Name} = new Collection<{arrayType}>();");
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
            CSharpEnum @enum = scope.AddEnum(contract.DefinitionName, CSharpModifiers.Public, contract.IsFlaggable ? "Flags" : null);
            foreach (EnumContractMember member in contract.Members)
            {
                @enum.AddMember(member.Name, member.Value)
                     .Inherits("int");
            }
        }

        private static bool TryGetArrayType(string type, out string arrayType)
        {
            int index = type.LastIndexOf('*');
            if (index >= 0)
            {
                arrayType = type.Substring(0, index);
                return true;
            }

            arrayType = null;
            return false;
        }
        #endregion
    }
}