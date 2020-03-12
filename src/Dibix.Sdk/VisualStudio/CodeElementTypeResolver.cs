using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using EnvDTE;

namespace Dibix.Sdk.VisualStudio
{
    internal sealed class CodeElementTypeResolver : ITypeResolver
    {
        #region Fields
        private readonly Lazy<ICollection<CodeElement>> _codeItemAccessor;
        private readonly Project _currentProject;
        private readonly ISchemaRegistry _schemaRegistry;
        #endregion

        #region Constructor
        public CodeElementTypeResolver(IServiceProvider serviceProvider, string executingFilePath, ISchemaRegistry schemaRegistry)
        {
            this._schemaRegistry = schemaRegistry;
            DTE dte = (DTE)serviceProvider.GetService(typeof(DTE));
            this._codeItemAccessor = new Lazy<ICollection<CodeElement>>(this.GetCodeItems);
            this._currentProject = VisualStudioExtensions.GetContainingProject(dte, executingFilePath);
        }
        #endregion

        #region ITypeResolver Members
        public TypeReference ResolveType(string input, string @namespace, string source, int line, int column, bool isEnumerable)
        {
            NullableTypeName typeName = input;

            CodeElement codeItem = this._codeItemAccessor.Value.FirstOrDefault(x => x.FullName == typeName.Name);
            if (codeItem == null)
                return null;

            TypeReference type = this.ResolveType(typeName, codeItem, source, line, column, isEnumerable);
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

        private TypeReference ResolveType(NullableTypeName typeName, CodeElement element, string source, int line, int column, bool isEnumerable)
        {
            switch (element)
            {
                case CodeEnum @enum:
                    return this.ResolveType(@enum.Namespace, element.Name, source, line, column, typeName.IsNullable, isEnumerable, (x, y) => new EnumSchema(x, y, false));

                case CodeClass @class:
                    return this.ResolveType(@class.Namespace, element.Name, source, line, column, typeName.IsNullable, isEnumerable, (x, y) =>
                    {
                        ObjectSchema schema = new ObjectSchema(x, y);
                        schema.Properties.AddRange(TraverseProperties(@class));
                        return schema;
                    });

                default:
                    throw new NotSupportedException($"Unsupported CodeElement type: {element.Kind}");
            }
        }

        private TypeReference ResolveType(CodeNamespace codeNamespace, string definitionName, string source, int line, int column, bool isNullable, bool isEnumerable, Func<string, string, SchemaDefinition> schemaDefinitionFactory)
        {
            SchemaTypeReference typeReference = SchemaTypeReference.WithNamespace(codeNamespace.FullName, definitionName, source, line, column, isNullable, isEnumerable);

            if (!this._schemaRegistry.IsRegistered(typeReference.Key))
                this._schemaRegistry.Populate(schemaDefinitionFactory(codeNamespace.FullName, definitionName));

            return typeReference;
        }

        private static IEnumerable<ObjectSchemaProperty> TraverseProperties(CodeClass @class)
        {
            foreach (CodeElement element in @class.Members.Cast<CodeElement>().Where(element => element.Kind == vsCMElement.vsCMElementProperty))
            {
                yield return new ObjectSchemaProperty(element.Name);
            }

            foreach (ObjectSchemaProperty property in @class.Bases.OfType<CodeClass>().SelectMany(TraverseProperties))
            {
                yield return property;
            }
        }
        #endregion
    }
}