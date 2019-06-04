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
                    CSharpClass @class = scope.AddClass(contract.DefinitionName, CSharpModifiers.Public | CSharpModifiers.Sealed);
                    ICollection<string> ctorAssignments = new Collection<string>();
                    foreach (ContractDefinitionProperty property in contract.Properties)
                    {
                        bool isEnumerable = TryGetArrayType(property.Type, out string arrayType);
                        @class.AddProperty(property.Name, !isEnumerable ? property.Type : $"ICollection<{arrayType}>")
                              .Getter(null)
                              .Setter(null, isEnumerable ? CSharpModifiers.Private : default);

                        if (isEnumerable)
                            ctorAssignments.Add($"this.{property.Name} = new Collection<{arrayType}>();");
                    }

                    if (!ctorAssignments.Any())
                        continue;

                    context.Output.AddUsing(typeof(ICollection<>).Namespace);
                    context.Output.AddUsing(typeof(Collection<>).Namespace);

                    @class.AddSeparator()
                          .AddConstructor(String.Join(Environment.NewLine, ctorAssignments));
                }
            }
        }
        #endregion

        #region Private Methods
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