using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class VisualStudioTypeLoader : ITypeLoader
    {
        #region Fields
        private readonly Lazy<ICollection<CodeElement>> _codeItemAccessor;
        private readonly Project _currentProject;
        #endregion

        #region Constructor
        public VisualStudioTypeLoader(IServiceProvider serviceProvider, string executingFilePath)
        {
            DTE dte = (DTE)serviceProvider.GetService(typeof(DTE));
            this._codeItemAccessor = new Lazy<ICollection<CodeElement>>(this.GetCodeItems);
            this._currentProject = VisualStudioExtensions.GetContainingProject(dte, executingFilePath);
        }
        #endregion

        #region ITypeLoader Members
        public TypeInfo LoadType(TypeName typeName, Action<string> errorHandler)
        {
            CodeElement codeItem = this._codeItemAccessor.Value.FirstOrDefault(x => x.FullName == typeName.NormalizedTypeName);
            if (codeItem == null)
            {
                errorHandler($"Could not resolve type '{typeName}'. Looking in current project.");
                return null;
            }

            TypeInfo type = CreateTypeInfo(typeName, codeItem);
            return type;
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

        private static TypeInfo CreateTypeInfo(TypeName typeName, CodeElement element)
        {
            bool isPrimitiveType = element.Kind == vsCMElement.vsCMElementEnum;
            TypeInfo type = new TypeInfo(typeName, isPrimitiveType);

            if (element is CodeClass @class)
            {
                IEnumerable<string> properties = TraverseProperties(@class);
                type.Properties.AddRange(properties);
            }

            return type;
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