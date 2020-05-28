using System;
using System.Collections.Concurrent;
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
        private readonly ReferencedAssemblyInspector _referencedAssemblyInspector;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;
        private readonly IDictionary<string, NeighborActionTarget> _neighborStatementMap;

        public ControllerActionTargetSelector(string productName, string areaName, string outputName, ICollection<SqlStatementInfo> statements, ReferencedAssemblyInspector referencedAssemblyInspector, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            this._productName = productName;
            this._areaName = areaName;
            this._outputName = outputName;
            this._statements = statements;
            this._referencedAssemblyInspector = referencedAssemblyInspector;
            this._schemaRegistry = schemaRegistry;
            this._logger = logger;
            this._neighborStatementMap = new ConcurrentDictionary<string, NeighborActionTarget>();
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
                this._logger.LogError(null, $@"Could not find action target: {target}
Tried: {normalizedNamespace}.{methodName}", filePath, lineInfo.LineNumber, lineInfo.LinePosition);
                return null;
            }

            // 3. Target 'could' be a compiled method in a neighbour project
            if (!this.TryGetNeighborActionTarget(methodName, filePath, lineInfo, out NeighborActionTarget neighborAction))
            {
                this._logger.LogError(null, $"Could not find a method name '{methodName}' on database accessor type '{typeName}'", filePath, lineInfo.LineNumber, lineInfo.LinePosition);
                return null;
            }

            return neighborAction;
        }

        private bool TryGetNeighborActionTarget(string methodName, string filePath, IJsonLineInfo lineInfo, out NeighborActionTarget target)
        {
            if (this._neighborStatementMap.TryGetValue(methodName, out target))
                return true;

            NeighborActionTarget neighborActionTarget = this._referencedAssemblyInspector.Inspect(referencedAssemblies =>
            {
                var query = from assembly in referencedAssemblies
                            where assembly.IsArtifactAssembly()
                            from type in assembly.GetTypes()
                            where type.IsDatabaseAccessor()
                            from method in type.GetMethods()
                            where method.Name == methodName
                            select this.CreateNeighborActionTarget(method, filePath, lineInfo);

                return query.FirstOrDefault();
            });

            if (neighborActionTarget != null)
            {
                this._neighborStatementMap.Add(neighborActionTarget.Name, neighborActionTarget);
                target = neighborActionTarget;
                return true;
            }

            return false;
        }

        private NeighborActionTarget CreateNeighborActionTarget(MethodInfo method, string filePath, IJsonLineInfo lineInfo)
        {
            IEnumerable<ParameterInfo> parameters = method.GetExternalParameters();

            TypeReference resultType = null;
            if (method.ReturnType != typeof(void))
                resultType = ReflectionTypeResolver.ResolveType(method.ReturnType, filePath, lineInfo.LineNumber, lineInfo.LinePosition, this._schemaRegistry);

            NeighborActionTarget target = new NeighborActionTarget(method.DeclaringType.FullName, resultType, method.Name);

            foreach (ParameterInfo parameter in parameters)
            {
                TypeReference parameterType = ReflectionTypeResolver.ResolveType(parameter.ParameterType, filePath, lineInfo.LineNumber, lineInfo.LinePosition, this._schemaRegistry);
                
                // ParameterInfo.HasDefaultValue/DefaultValue => It is illegal to reflect on the custom attributes of a Type loaded via ReflectionOnlyGetType (see Assembly.ReflectionOnly) -- use CustomAttributeData instead
                bool hasDefaultValue = parameter.RawDefaultValue != DBNull.Value;
                object defaultValue = parameter.RawDefaultValue;
                target.Parameters.Add(parameter.Name, new ActionParameter(parameter.Name, parameterType, hasDefaultValue, defaultValue));
            }
            return target;
        }
    }
}