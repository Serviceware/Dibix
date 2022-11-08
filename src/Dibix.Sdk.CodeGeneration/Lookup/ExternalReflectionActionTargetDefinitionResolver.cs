using System;
using System.Collections.Generic;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    // Target is a reflection target within a foreign assembly
    internal sealed class ExternalReflectionActionTargetDefinitionResolver : ActionTargetDefinitionResolver
    {
        private const string LockSectionName = "ExternalReflectionTarget";
        private readonly ILockEntryManager _lockEntryManager;

        public ExternalReflectionActionTargetDefinitionResolver(ISchemaDefinitionResolver schemaDefinitionResolver, ISchemaRegistry schemaRegistry, ILockEntryManager lockEntryManager, ILogger logger) : base(schemaDefinitionResolver, schemaRegistry, logger)
        {
            this._lockEntryManager = lockEntryManager;
        }

        public override bool TryResolve<T>(string targetName, string filePath, int line, int column, IDictionary<string, ExplicitParameter> explicitParameters, IDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters, out T actionTargetDefinition)
        {
            string[] parts = targetName.Split(',');
            if (parts.Length != 2)
            {
                actionTargetDefinition = null;
                return false;
            }

            if (!this._lockEntryManager.HasEntry(LockSectionName, targetName))
            {
                base.Logger.LogError("Reflection targets are not supported anymore", filePath, line, column);
                actionTargetDefinition = null;
                return false;
            }

            string assemblyName = parts[1];
            int methodNameIndex = parts[0].LastIndexOf('.');
            string typeName = parts[0].Substring(0, methodNameIndex);
            string methodName = parts[0].Substring(methodNameIndex + 1, parts[0].Length - methodNameIndex - 1);

            /*
            if (!this._assemblyResolver.TryGetAssembly(assemblyName, out Assembly assembly))
            {
                base.Logger.LogError(null, $"Could not locate assembly: {assemblyName}", filePath, line, column + parts[0].Length + 1);
                actionDefinition = null;
                return true;
            }

            Type type = assembly.GetType(typeName, true);
            MethodInfo method = type.GetMethod(methodName);
            if (method == null)
            {
                base.Logger.LogError(null, $"Could not find method: {methodName} on {typeName}", filePath, line, column + methodNameIndex + 1);
                actionDefinition = null;
                return true;
            }

            actionDefinition = ReflectionOnlyTypeInspector.Inspect(() => this.CreateActionDefinition(targetName, assemblyName, method, filePath, line, column, explicitParameters, pathParameters, bodyParameters));
            */

            actionTargetDefinition = new T();
            actionTargetDefinition.Target = new ReflectionActionTarget(assemblyName, typeName, methodName, isAsync: false, hasRefParameters: false, filePath, line, column);
            ActionParameterRegistry parameterRegistry = new ActionParameterRegistry(actionTargetDefinition, pathParameters);
            foreach (ExplicitParameter parameter in explicitParameters.Values)
            {
                string apiParameterName = parameter.Name;
                string internalParameterName = apiParameterName;
                ActionParameterSource source = parameter.SourceBuilder.Build(type: null);
                ActionParameterLocation location = ResolveParameterLocationFromSource(source, ref apiParameterName);
                if (location == ActionParameterLocation.Path)
                    pathParameters.Remove(apiParameterName);

                TypeReference type = null;
                ValueReference defaultValue = null;
                if (location == ActionParameterLocation.Header)
                {
                    // Generate a null default value for header parameters
                    PrimitiveTypeReference primitiveTypeReference = new PrimitiveTypeReference(PrimitiveType.String, isNullable: true, isEnumerable: false, filePath, parameter.Line, parameter.Column);
                    type = primitiveTypeReference;
                    defaultValue = new NullValueReference(primitiveTypeReference, filePath, parameter.Line, parameter.Column);
                }

                bool isRequired = base.IsParameterRequired(type, location, defaultValue);
                parameterRegistry.Add(new ActionParameter(apiParameterName, internalParameterName, type, location, isRequired, defaultValue, source, filePath, parameter.Line, parameter.Column));
            }

            foreach (PathParameter pathParameter in pathParameters.Values)
            {
                TypeReference typeReference = new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false, filePath, pathParameter.Line, pathParameter.Column);
                const ActionParameterLocation location = ActionParameterLocation.Path;
                bool isRequired = base.IsParameterRequired(type: null, location, defaultValue: null);
                ActionParameter parameter = new ActionParameter(pathParameter.Name, pathParameter.Name, typeReference, location, isRequired, defaultValue: null, source: null, filePath, pathParameter.Line, pathParameter.Column);
                actionTargetDefinition.Parameters.Add(parameter);
            }

            return true;
        }

        private static ActionParameterLocation ResolveParameterLocationFromSource(ActionParameterSource parameterSource, ref string apiParameterName)
        {
            switch (parameterSource)
            {
                case ActionParameterBodySource _: return ActionParameterLocation.Body;

                case ActionParameterConstantSource _: return ActionParameterLocation.NonUser;

                case ActionParameterPropertySource actionParameterPropertySource:
                    ActionParameterLocation location = ActionParameterLocation.NonUser;
                    apiParameterName = actionParameterPropertySource.PropertyName.Split('.')[0];
                    IsUserParameter(actionParameterPropertySource.Definition, actionParameterPropertySource.PropertyName, ref location, ref apiParameterName);
                    return location;

                default: throw new ArgumentOutOfRangeException(nameof(parameterSource), parameterSource, null);
            }
        }
    }
}