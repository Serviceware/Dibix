using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ControllerActionTargetSelector : IControllerActionTargetSelector
    {
        private readonly string _productName;
        private readonly string _areaName;
        private readonly string _outputName;
        private readonly ICollection<SqlStatementInfo> _statements;
        private readonly IErrorReporter _errorReporter;
        private readonly Lazy<IDictionary<string, string>> _neighborStatementMapAccessor;

        public ControllerActionTargetSelector(string productName, string areaName, string outputName, ICollection<SqlStatementInfo> statements, IReferencedAssemblyProvider referencedAssemblyProvider, IErrorReporter errorReporter)
        {
            this._productName = productName;
            this._areaName = areaName;
            this._outputName = outputName;
            this._statements = statements;
            this._errorReporter = errorReporter;
            this._neighborStatementMapAccessor = new Lazy<IDictionary<string, string>>(() => CreateNeighborStatementMap(referencedAssemblyProvider));
        }

        public ActionDefinitionTarget Select(string target, string filePath, IJsonLineInfo lineInfo)
        {
            // 1. Target is a reflection target within a foreign assembly
            bool isExternal = target.Contains(",");
            if (isExternal)
                return new ReflectionActionTarget(target);

            // Use explicit namespace if it can be extracted
            int statementNameIndex = target.LastIndexOf('.');
            string @namespace = statementNameIndex >= 0 ? target.Substring(0, statementNameIndex) : null;

            // Detect absolute namespace if it is prefixed with the product name
            // i.E.: Data.Runtime is a relative namespace
            bool isAbsolute = target.StartsWith($"{this._productName}.", StringComparison.Ordinal);
            string normalizedNamespace = @namespace;
            if (!isAbsolute)
                normalizedNamespace = NamespaceUtility.BuildAbsoluteNamespace(this._productName, this._areaName, LayerName.Data, @namespace);

            string typeName = $"{normalizedNamespace}.{this._outputName}";
            string methodName = target.Substring(statementNameIndex + 1);

            // 2. Target is a SQL statement within the current project
            SqlStatementInfo statement = this._statements.FirstOrDefault(x => x.Namespace == normalizedNamespace && x.Name == methodName);
            if (statement != null)
                return new LocalActionTarget(statement, this._outputName);

            // Relative namespaces can not be resolved in neighbor projects
            if (!isAbsolute)
            {
                this._errorReporter.RegisterError(filePath, lineInfo.LineNumber, lineInfo.LinePosition, null, $@"Could not find action target: {target}
Tried: {normalizedNamespace}.{methodName}");
                return null;
            }

            // 3. Target 'could' be a compiled method in a neighbour project
            if (!this._neighborStatementMapAccessor.Value.TryGetValue(methodName, out string neighborTypeName))
            {
                this._errorReporter.RegisterError(filePath, lineInfo.LineNumber, lineInfo.LinePosition, null, $"Could not find a method name '{methodName}' on database accessor type '{typeName}'");
                return null;
            }

            return new ExternalActionTarget(neighborTypeName, methodName);
        }

        private static IDictionary<string, string> CreateNeighborStatementMap(IReferencedAssemblyProvider referencedAssemblyProvider) => CreateNeighborStatementMapCore(referencedAssemblyProvider).ToDictionary(x => x.Key, x => x.Value);

        private static IEnumerable<KeyValuePair<string, string>> CreateNeighborStatementMapCore(IReferencedAssemblyProvider referencedAssemblyProvider)
        {
            return from assembly in referencedAssemblyProvider.ReferencedAssemblies
                   where CustomAttributeData.GetCustomAttributes(assembly).Any(x => x.AttributeType.FullName == "Dibix.ArtifactAssemblyAttribute")
                   from type in assembly.GetTypes() 
                   where CustomAttributeData.GetCustomAttributes(type)
                                            .Any(x => x.AttributeType.FullName == "Dibix.DatabaseAccessorAttribute") 
                   from method in type.GetMethods() 
                   let parameters = method.GetParameters() 
                   where parameters.Any() && parameters[0].ParameterType.FullName == "Dibix.IDatabaseAccessorFactory" 
                   select new KeyValuePair<string, string>(method.Name, type.FullName);
        }
    }
}