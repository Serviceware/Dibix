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

        public ExternalReflectionActionTargetDefinitionResolver(ISchemaRegistry schemaRegistry, ILockEntryManager lockEntryManager, ILogger logger) : base(schemaRegistry, logger)
        {
            this._lockEntryManager = lockEntryManager;
        }

        public override bool TryResolve<T>(string targetName, SourceLocation sourceLocation, IReadOnlyDictionary<string, ExplicitParameter> explicitParameters, IReadOnlyDictionary<string, PathParameter> pathParameters, ICollection<string> bodyParameters, out T actionTargetDefinition)
        {
            string[] parts = targetName.Split(',');
            if (parts.Length != 2)
            {
                actionTargetDefinition = null;
                return false;
            }

            if (!this._lockEntryManager.HasEntry(LockSectionName, targetName))
            {
                base.Logger.LogError("Reflection targets are not supported anymore", sourceLocation.Source, sourceLocation.Line, sourceLocation.Column);
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
            actionTargetDefinition.Target = new ReflectionActionTarget(assemblyName, typeName, methodName, isAsync: false, hasRefParameters: false, sourceLocation);
            ActionParameterRegistry parameterRegistry = new ActionParameterRegistry(actionTargetDefinition, pathParameters);
            foreach (ExplicitParameter parameter in explicitParameters.Values)
            {
                if (parameter is not ParameterDescriptor parameterDescriptor)
                {
                    Logger.LogError($"Metadata of parameter '{parameter.Name}' cannot be automatically detected for this action target and therefore must be specified explicitly", parameter.SourceLocation);
                    continue;
                }

                string apiParameterName = parameter.Name;
                string internalParameterName = apiParameterName;
                ActionParameterSource source = parameterDescriptor.SourceBuilder?.Build(type: null);
                ActionParameterLocation location = ResolveParameterLocationFromSource(source, parameterDescriptor.ParameterLocation, ref apiParameterName);

                TypeReference type = parameterDescriptor.Type;
                ValueReference defaultValue = parameterDescriptor.DefaultValue;

                bool isRequired = base.IsParameterRequired(type, location, defaultValue);
                bool isOutput = false; // Not supported
                parameterRegistry.Add(new ActionParameter(apiParameterName, internalParameterName, type, location, isRequired, isOutput, defaultValue, source, new SourceLocation(sourceLocation.Source, parameter.SourceLocation.Line, parameter.SourceLocation.Column)));
            }

            foreach (PathParameter pathParameter in pathParameters.Values)
            {
                TypeReference typeReference = new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false, size: null, location: new SourceLocation(sourceLocation.Source, pathParameter.Location.Line, pathParameter.Location.Column));
                const ActionParameterLocation location = ActionParameterLocation.Path;
                bool isRequired = base.IsParameterRequired(type: null, location, defaultValue: null);
                bool isOutput = false; // Not supported
                ActionParameter parameter = new ActionParameter(pathParameter.Name, pathParameter.Name, typeReference, location, isRequired, isOutput, defaultValue: null, source: null, new SourceLocation(sourceLocation.Source, pathParameter.Location.Line, pathParameter.Location.Column));
                actionTargetDefinition.Parameters.Add(parameter);
            }

            return true;
        }

        private static ActionParameterLocation ResolveParameterLocationFromSource(ActionParameterSource parameterSource, ActionParameterLocation? explicitLocation, ref string apiParameterName)
        {
            switch (parameterSource)
            {
                case ActionParameterBodySource _: return ActionParameterLocation.Body;

                case ActionParameterConstantSource _: return ActionParameterLocation.NonUser;
                
                case ActionParameterClaimSource _: return ActionParameterLocation.NonUser;

                case ActionParameterPropertySource actionParameterPropertySource:
                    ActionParameterLocation location = ActionParameterLocation.NonUser;
                    apiParameterName = actionParameterPropertySource.PropertyName.Split('.')[0];
                    IsUserParameter(actionParameterPropertySource.Definition, actionParameterPropertySource.PropertyName, ref location, ref apiParameterName);
                    return location;

                case null when explicitLocation is not null: return explicitLocation.Value;
                
                case null: return ActionParameterLocation.Query;

                default: throw new ArgumentOutOfRangeException(nameof(parameterSource), parameterSource, null);
            }
        }
    }
}