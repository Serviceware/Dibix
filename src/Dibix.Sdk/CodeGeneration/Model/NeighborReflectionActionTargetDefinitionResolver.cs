using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    // Target is a compiled method in a neighbour project
    internal sealed class NeighborReflectionActionTargetDefinitionResolver : ActionTargetDefinitionResolver
    {
        #region Fields
        private readonly string _projectName;
        private readonly ReferencedAssemblyInspector _referencedAssemblyInspector;
        #endregion

        #region Constructor
        public NeighborReflectionActionTargetDefinitionResolver
        (
            string projectName
          , ReferencedAssemblyInspector referencedAssemblyInspector
          , ISchemaDefinitionResolver schemaDefinitionResolver
          , ISchemaRegistry schemaRegistry
          , ILogger logger
        ) : base(schemaDefinitionResolver, schemaRegistry, logger)
        {
            this._projectName = projectName;
            this._referencedAssemblyInspector = referencedAssemblyInspector;
        }
        #endregion

        #region Overrides
        public override bool TryResolve<T>(string targetName, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters, out T actionTargetDefinition)
        {
            int statementNameIndex = targetName.LastIndexOf('.');
            string methodName = targetName.Substring(statementNameIndex + 1);

            actionTargetDefinition = this._referencedAssemblyInspector.Inspect(referencedAssemblies =>
            {
                var query = from assembly in referencedAssemblies
                            where assembly.IsArtifactAssembly()
                            from type in assembly.GetTypes()
                            where type.IsDatabaseAccessor()
                            from method in type.GetMethods()
                            where method.Name == methodName
                               || method.Name == $"{methodName}Async"
                            select this.CreateActionTargetDefinition<T>(targetName, assemblyName: null, method, filePath, line, column, explicitParameters, pathParameters, bodyParameters);

                return query.FirstOrDefault();
            });

            return actionTargetDefinition != null;
        }
        #endregion

        #region Private Methods
        private T CreateActionTargetDefinition<T>(string targetName, string assemblyName, MethodInfo method, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters) where T : ActionTargetDefinition, new()
        {
            string operationName = method.Name;
            bool isReflectionTarget = assemblyName != null;
            Type returnType = this.CollectReturnType(method, isReflectionTarget);
            bool isAsync = returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>);
            bool hasRefParameters = method.GetParameters().Any(x => x.ParameterType.IsByRef);

            if (isAsync)
            {
                returnType = returnType.GenericTypeArguments[0];
                const string asyncSuffix = "Async";
                if (operationName.EndsWith(asyncSuffix, StringComparison.Ordinal))
                    operationName = operationName.Remove(operationName.Length - asyncSuffix.Length);
            }

            TypeReference resultType = null;
            if (returnType != typeof(void))
                resultType = ReflectionTypeResolver.ResolveType(returnType, filePath, line, column, base.SchemaRegistry, base.Logger);

            NeighborActionTarget target;
            string accessorFullName = method.DeclaringType.FullName;
            if (isReflectionTarget)
                target = new ReflectionActionTarget(assemblyName, accessorFullName, operationName, isAsync, hasRefParameters, filePath, line, column);
            else
                target = new NeighborActionTarget(accessorFullName, operationName, isAsync, hasRefParameters, filePath, line, column);

            T actionTargetDefinition = new T();
            actionTargetDefinition.Target = target;
            ActionParameterRegistry parameterRegistry = new ActionParameterRegistry(actionTargetDefinition, pathParameters);
            method.CollectErrorResponses((statusCode, errorCode, errorDescription) => RegisterErrorResponse(actionTargetDefinition, statusCode, errorCode, errorDescription));
            actionTargetDefinition.DefaultResponseType = resultType;

            IEnumerable<ParameterInfo> parameters = this.CollectReflectionInfo(() => method.GetExternalParameters(isAsync), isReflectionTarget, Enumerable.Empty<ParameterInfo>);
            foreach (ParameterInfo parameter in parameters)
            {
                string parameterName = parameter.Name;
                Type normalizedParameterType = parameter.ParameterType;
                if (normalizedParameterType.HasElementType)
                    normalizedParameterType = normalizedParameterType.GetElementType(); // By-ref types are not supported

                TypeReference parameterType = ReflectionTypeResolver.ResolveType(normalizedParameterType, filePath, line, column, base.SchemaRegistry, base.Logger);
                if (parameter.IsNullable())
                    parameterType.IsNullable = true;

                // ParameterInfo.HasDefaultValue/DefaultValue => It is illegal to reflect on the custom attributes of a Type loaded via ReflectionOnlyGetType (see Assembly.ReflectionOnly) -- use CustomAttributeData instead
                ValueReference defaultValue = null;
                if (parameter.RawDefaultValue != DBNull.Value)
                    defaultValue = RawValueReferenceParser.Parse(parameterType, parameter.RawDefaultValue, filePath, line, column, base.Logger);

                bool isOutParameter = parameter.IsOut;

                base.CollectActionParameter
                (
                    parameterName
                  , parameterType
                  , defaultValue
                  , isOutParameter
                  , targetName
                  , filePath
                  , line
                  , column
                  , parameterRegistry
                  , explicitParameters
                  , pathParameters
                  , bodyParameters
                );
            }

            return actionTargetDefinition;
        }

        private Type CollectReturnType(MethodInfo method, bool isReflectionTarget)
        {
            Type returnType = this.CollectReflectionInfo(() => method.ReturnType, isReflectionTarget, () => typeof(void));
            if (isReflectionTarget && returnType.FullName == typeof(HttpResponseMessage).FullName)
                return typeof(void);

            return returnType;
        }

        private T CollectReflectionInfo<T>(Func<T> valueResolver, bool isReflectionTarget, Func<T> fallbackValueResolver)
        {
            if (!isReflectionTarget)
                return valueResolver();

            try
            {
                return valueResolver();
            }
            catch (FileNotFoundException exception)
            {
                AssemblyName assemblyName = new AssemblyName(exception.FileName);
                if (assemblyName.Name == this._projectName)
                    return fallbackValueResolver();

                throw;
            }
        }
        #endregion
    }
}