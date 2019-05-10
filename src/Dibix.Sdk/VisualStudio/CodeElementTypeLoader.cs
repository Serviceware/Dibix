using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using EnvDTE;

namespace Dibix.Sdk.VisualStudio
{
    internal sealed class CodeElementContractResolver : IContractResolver
    {
        #region Fields
        private readonly Lazy<ICollection<CodeElement>> _codeItemAccessor;
        private readonly Project _currentProject;
        #endregion

        #region Constructor
        public CodeElementContractResolver(IServiceProvider serviceProvider, string executingFilePath)
        {
            DTE dte = (DTE)serviceProvider.GetService(typeof(DTE));
            this._codeItemAccessor = new Lazy<ICollection<CodeElement>>(this.GetCodeItems);
            this._currentProject = VisualStudioExtensions.GetContainingProject(dte, executingFilePath);
        }
        #endregion

        #region IContractResolver Members
        public ContractInfo ResolveContract(string input, Action<string> errorHandler)
        {
            ContractName name = new ContractName(input);
            CodeElement codeItem = this._codeItemAccessor.Value.FirstOrDefault(x => x.FullName == name.TypeName);
            if (codeItem == null)
            {
                errorHandler($"Could not resolve type '{name}'. Looking in current project.");
                return null;
            }

            ContractInfo contract = CreateContractInfo(name, codeItem);
            return contract;
        }
        #endregion

        #region Private Methods
        private ICollection<CodeElement> GetCodeItems()
        {
            ICollection<CodeElement> codeItems = this._currentProject
                                                     .ProjectItems
                                                     .GetChildren(true)
                                                     .Where(item => item.FileCodeModel != null)
                                                     .SelectMany(item => TraverseTypes(item.FileCodeModel.CodeElements, vsCMElement.vsCMElementClass, vsCMElement.vsCMElementEnum))
                                                     .ToArray();

            return codeItems;
        }

        private static IEnumerable<CodeElement> TraverseTypes(CodeElements parent, params vsCMElement[] kinds)
        {
            foreach (CodeElement elem in parent)
            {
                if (elem.Kind == vsCMElement.vsCMElementNamespace)
                {
                    foreach (CodeElement element in TraverseTypes(((CodeNamespace)elem).Members, kinds))
                    {
                        yield return element;
                    }
                }

                if (kinds.Contains(elem.Kind))
                {
                    yield return elem;
                }
            }
        }

        private static ContractInfo CreateContractInfo(ContractName contractName, CodeElement element)
        {
            bool isPrimitiveType = element.Kind == vsCMElement.vsCMElementEnum;
            ContractInfo contract = new ContractInfo(contractName, isPrimitiveType);

            if (element is CodeClass @class)
            {
                IEnumerable<string> properties = TraverseProperties(@class);
                contract.Properties.AddRange(properties);
            }

            return contract;
        }

        private static IEnumerable<string> TraverseProperties(CodeClass @class)
        {
            foreach (CodeElement element in @class.Members.Cast<CodeElement>().Where(element => element.Kind == vsCMElement.vsCMElementProperty))
            {
                yield return element.Name;
            }

            foreach (string property in @class.Bases.OfType<CodeClass>().SelectMany(TraverseProperties))
            {
                yield return property;
            }
        }
        #endregion
    }
}