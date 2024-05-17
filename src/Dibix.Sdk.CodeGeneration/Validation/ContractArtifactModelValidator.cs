using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractArtifactModelValidator : ICodeGenerationModelValidator
    {
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;

        public ContractArtifactModelValidator(ISchemaRegistry schemaRegistry, ILogger logger)
        {
            _schemaRegistry = schemaRegistry;
            _logger = logger;
        }

        public bool Validate(CodeGenerationModel model)
        {
            bool isValid = ValidateUnusedContracts(model);
            return isValid;
        }

        private bool ValidateUnusedContracts(CodeGenerationModel model)
        {
            IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap = model.Schemas
                                                                                         .Where(x => x.Source == SchemaDefinitionSource.Defined)
                                                                                         .OfType<ObjectSchema>()
                                                                                         .Select(x => new ObjectContractDefinition(x))
                                                                                         .ToDictionary(x => x.Schema);

            ValidateStatementContracts(model, schemaPropertyMap);
            ValidateEndpointContracts(model, schemaPropertyMap);

            bool isValid = true;
            foreach (KeyValuePair<ObjectSchema, ObjectContractDefinition> keyValuePair in schemaPropertyMap)
            {
                ObjectSchema objectSchema = keyValuePair.Key;
                ObjectContractDefinition objectContractDefinition = keyValuePair.Value;
                bool allPropertiesUnused = !objectSchema.Properties.Except(objectContractDefinition.Properties).Any();
                if (allPropertiesUnused)
                {
                    _logger.LogError($"Unused contract definition: {objectSchema.FullName}", objectContractDefinition.Schema.Location.Source, objectContractDefinition.Schema.Location.Line, objectContractDefinition.Schema.Location.Column);
                    continue;
                }
                
                foreach (ObjectSchemaProperty property in objectContractDefinition.Properties)
                {
                    isValid = false;
                    Token<string> name = property.Name;
                    _logger.LogError($"Unused contract definition property: {objectSchema.FullName}.{name.Value}", name.Location.Source, name.Location.Line, name.Location.Column);
                }
            }
            
            return isValid;
        }

        private void ValidateStatementContracts(IPersistedCodeGenerationModel model, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap)
        {
            foreach (SqlStatementDefinition statement in model.Schemas.Where(x => x.Source == SchemaDefinitionSource.Defined).OfType<SqlStatementDefinition>())
            {
                (ObjectSchema gridResultSchema, IDictionary<string, ObjectSchemaProperty> gridResultSchemaPropertyMap, ICollection<ObjectSchemaProperty> gridResultSchemaProperties) = CollectGridResultInfos(statement, schemaPropertyMap);

                for (int i = 0; i < statement.Results.Count; i++)
                {
                    SqlQueryResult result = statement.Results[i];
                    ValidateResultSchemaProperties(result, schemaPropertyMap);
                    ValidateGridResultSchemaProperties(statement, result, i, gridResultSchema, gridResultSchemaPropertyMap, gridResultSchemaProperties);
                }
            }
        }

        private void ValidateResultSchemaProperties(SqlQueryResult result, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap)
        {
            // These types will automatically be mapped on an existing property of the same type
            ICollection<string> multiMapTypes = new HashSet<string>(result.Types.Select(BuildKey));

            foreach (TypeReference type in result.Types)
            {
                ValidateResultSchemaProperties(result, type, multiMapTypes, schemaPropertyMap);
            }

            if (result.ProjectToType != null)
            {
                ValidateResultSchemaProperties(result, result.ProjectToType, multiMapTypes, schemaPropertyMap);
            }
        }

        private void ValidateResultSchemaProperties(SqlQueryResult result, TypeReference type, ICollection<string> multiMapTypes, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap)
        {
            if (!(type is SchemaTypeReference resultSchemaTypeReference))
                return;

            if (_schemaRegistry.GetSchema(resultSchemaTypeReference) is not ObjectSchema objectSchema)
                return;

            // Some properties in Dibix.FileEntity for example are optional
            if (objectSchema.Source == SchemaDefinitionSource.Internal)
                return;

            if (!schemaPropertyMap.TryGetValue(objectSchema, out ObjectContractDefinition objectContractDefinition))
                return;

            foreach (ObjectSchemaProperty property in objectSchema.Properties)
            {
                if (property.Type == null)
                    continue;

                if (!IsPropertyUsed(result, property, multiMapTypes))
                    continue;

                VisitProperty(objectContractDefinition.Properties, property);
            }
        }

        private static void ValidateGridResultSchemaProperties(SqlStatementDefinition statement, SqlQueryResult result, int index, SchemaDefinition schema, IDictionary<string, ObjectSchemaProperty> propertyMap, ICollection<ObjectSchemaProperty> properties)
        {
            if (statement.MergeGridResult && index == 0)
                return;

            if (properties == null)
                return;

            string resultName = result.Name.Value;
            if (!propertyMap.TryGetValue(resultName, out ObjectSchemaProperty property))
                throw new InvalidOperationException($"Missing grid result property on type '{schema.FullName}' for result '{resultName}'");

            VisitProperty(properties, property);
        }

        private void ValidateEndpointContracts(CodeGenerationModel model, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap)
        {
            foreach (ControllerDefinition controller in model.Controllers)
            {
                foreach (ActionDefinition action in controller.Actions)
                {
                    ValidateBodyContract(action, schemaPropertyMap);
                    ValidateResponseContracts(action, schemaPropertyMap);
                }
            }
        }

        private void ValidateBodyContract(ActionDefinition action, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap)
        {
            if (action.RequestBody?.Contract is not SchemaTypeReference schemaTypeReference)
                return;

            if (_schemaRegistry.GetSchema(schemaTypeReference) is not ObjectSchema objectSchema)
                return;

            if (!schemaPropertyMap.TryGetValue(objectSchema, out ObjectContractDefinition objectContractDefinition))
                return;

            switch (action.Target)
            {
                case LocalActionTarget localActionTarget:
                    VisitLocalActionTarget(action, localActionTarget, objectSchema, objectContractDefinition.Properties, schemaPropertyMap);
                    break;

                case ReflectionActionTarget:
                    VisitReflectionActionTarget(objectSchema, schemaPropertyMap);
                    break;
            }
        }

        private void ValidateResponseContracts(ActionTargetDefinition actionTargetDefinition, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap)
        {
            foreach (ActionResponse response in actionTargetDefinition.Responses.Values)
            {
                if (response.ResultType == null)
                    continue;

                VisitAllPropertiesNested(response.ResultType, schemaPropertyMap);
            }
        }

        private void VisitLocalActionTarget(ActionTargetDefinition actionTargetDefinition, LocalActionTarget localActionTarget, ObjectSchema bodySchema, ICollection<ObjectSchemaProperty> bodyProperties, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap)
        {
            IDictionary<string, UserDefinedTypeParameter?> parameters = localActionTarget.SqlStatementDefinition.Parameters.ToDictionary(x => x.Name, CollectUDTParameter, StringComparer.OrdinalIgnoreCase);
            foreach (ActionParameter actionParameter in actionTargetDefinition.Parameters)
            {
                if (parameters.TryGetValue(actionParameter.InternalParameterName, out UserDefinedTypeParameter? userDefinedTypeParameter))
                {
                    parameters.Remove(actionParameter.InternalParameterName);

                    // The ApiParameterName might be the same for multiple target parameters, when mapping from a complex object
                    if (!parameters.ContainsKey(actionParameter.ApiParameterName))
                        parameters.Add(actionParameter.ApiParameterName, userDefinedTypeParameter);
                }

                VisitParameterSource(actionParameter.Type, actionParameter.ParameterSource, bodySchema, bodyProperties, schemaPropertyMap);
            }

            // Visit target parameters that are mapped from body contract properties
            foreach (ObjectSchemaProperty property in bodySchema.Properties)
            {
                bool hasMatchingParameter = parameters.TryGetValue(property.Name, out UserDefinedTypeParameter? userDefinedTypeParameter);
                if (!hasMatchingParameter)
                    continue;

                VisitProperty(bodyProperties, property);
                VisitUDTParameter(userDefinedTypeParameter, bodySchema, property, schemaPropertyMap);
            }
        }

        private void VisitReflectionActionTarget(SchemaDefinition bodySchema, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap)
        {
            // Visit all properties of the body, if the target is not visible to us
            VisitAllPropertiesNested(bodySchema, schemaPropertyMap);
        }

        private UserDefinedTypeParameter? CollectUDTParameter(SqlQueryParameter parameter)
        {
            if (parameter.Type is SchemaTypeReference schemaTypeReference && _schemaRegistry.GetSchema(schemaTypeReference) is UserDefinedTypeSchema userDefinedTypeSchema)
                return new UserDefinedTypeParameter(parameter.Name, userDefinedTypeSchema);

            return null;
        }

        private void VisitParameterSource(TypeReference parameterType, ActionParameterSource parameterSource, ObjectSchema bodySchema, ICollection<ObjectSchemaProperty> bodyProperties, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap)
        {
            switch (parameterSource)
            {
                case ActionParameterBodySource bodySource:
                    VisitBodySource(bodySource, bodySchema, schemaPropertyMap);
                    break;

                case ActionParameterPropertySource propertySource:
                    VisitPropertySource(parameterType, propertySource, bodySchema, bodyProperties, schemaPropertyMap);
                    break;
            }
        }

        private void VisitBodySource(ActionParameterBodySource bodySource, SchemaDefinition bodySchema, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap)
        {
            if (bodySource.ConverterName == null)
                return;

            // Visit all properties of the body, if they are used within a converter, that is not visible to us
            VisitAllPropertiesNested(bodySchema, schemaPropertyMap);
        }

        private void VisitPropertySource(TypeReference parameterType, ActionParameterPropertySource propertySource, ObjectSchema bodySchema, ICollection<ObjectSchemaProperty> bodyProperties, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap)
        {
            foreach (ActionParameterPropertySourceNode node in propertySource.Nodes)
            {
                // Probably a non-user property parameter source
                if (node.Schema == null || node.Property is not ObjectSchemaProperty objectSchemaProperty)
                    continue;

                if (!schemaPropertyMap.TryGetValue(node.Schema, out ObjectContractDefinition objectContractDefinition))
                    continue;

                VisitProperty(objectContractDefinition.Properties, objectSchemaProperty);
            }

            foreach (ActionParameterItemSource itemSource in propertySource.ItemSources)
            {
                VisitParameterSource(parameterType, itemSource.Source, bodySchema, bodyProperties, schemaPropertyMap);
            }
        }

        private void VisitUDTParameter(UserDefinedTypeParameter? userDefinedTypeParameter, SchemaDefinition bodySchema, ObjectSchemaProperty bodyProperty, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap)
        {
            if (userDefinedTypeParameter == null) 
                return;

            string parameterName = userDefinedTypeParameter.Value.Name;
            string udtName = userDefinedTypeParameter.Value.Schema.UdtName;
            if (!(bodyProperty.Type is SchemaTypeReference schemaTypeReference))
            {
                // UDTs with only one column can be mapped from a primitive type
                if (userDefinedTypeParameter.Value.Schema.Properties.Count == 1)
                    return;

                _logger.LogError($"Unexpected property contract '{bodyProperty.Type?.GetType()}' for property '{bodySchema.FullName}.{bodyProperty.Name}'. Expected object schema when mapping complex UDT parameter: @{parameterName} {udtName}.", bodyProperty.Type.Location.Source, bodyProperty.Type.Location.Line, bodyProperty.Type.Location.Column);
                return;
            }

            SchemaDefinition schemaDefinition = _schemaRegistry.GetSchema(schemaTypeReference);
            if (!(schemaDefinition is ObjectSchema propertySchema))
            {
                _logger.LogError($"Unexpected property contract '{schemaDefinition?.GetType()}' for property '{bodySchema.FullName}.{bodyProperty.Name}'. Expected object schema when mapping complex UDT parameter: @{parameterName} {udtName}.", bodyProperty.Type.Location.Source, bodyProperty.Type.Location.Line, bodyProperty.Type.Location.Column);
                return;
            }

            ICollection<string> udtParameters = new HashSet<string>(userDefinedTypeParameter.Value.Schema.Properties.Select(x => x.Name.Value), StringComparer.OrdinalIgnoreCase);
            if (!schemaPropertyMap.TryGetValue(propertySchema, out ObjectContractDefinition objectContractDefinition))
                return;

            // Mark all nested source properties as used, if a matching column on the UDT exists
            foreach (ObjectSchemaProperty property in propertySchema.Properties)
            {
                if (udtParameters.Contains(property.Name))
                    VisitProperty(objectContractDefinition.Properties, property);
            }
        }

        private void VisitAllPropertiesNested(SchemaDefinition schema, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap) => VisitAllPropertiesNested(schema, schemaPropertyMap, (x, y) => x.Accept(y));
        private void VisitAllPropertiesNested(TypeReference type, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap) => VisitAllPropertiesNested(type, schemaPropertyMap, (x, y) => x.Accept(y));
        private void VisitAllPropertiesNested<T>(T node, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap, Action<SchemaVisitor, T> visitorHandler)
        {
            ValidationSchemaVisitor visitor = new ValidationSchemaVisitor(_schemaRegistry, schemaPropertyMap, VisitProperty);
            visitorHandler(visitor, node);
        }

        private static void VisitProperty(ICollection<ObjectSchemaProperty> properties, ObjectSchemaProperty property)
        {
            _ = properties.Remove(property);
            //if (!properties.Remove(property))
            //    throw new InvalidOperationException($"Property instance '{property.Name}' is not part of the source collection");
        }

        private static bool IsPropertyUsed(SqlQueryResult result, ObjectSchemaProperty property, ICollection<string> multiMapTypes)
        {
            if (result.Columns.Contains(property.Name)) 
                return true;

            if (multiMapTypes.Contains(BuildKey(property.Type)))
                return true;

            return false;
        }

        private (ObjectSchema schema, IDictionary<string, ObjectSchemaProperty> propertyMap, ICollection<ObjectSchemaProperty> visitedProperties) CollectGridResultInfos(SqlStatementDefinition statement, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap)
        {
            if (!statement.IsGridResult())
                return (null, null, null);

            if (!(statement.ResultType is SchemaTypeReference gridResultSchemaTypeReference))
            {
                throw new InvalidOperationException($"Unexpected grid result type: {statement.ResultType.GetType()}");
            }

            SchemaDefinition schemaDefinition = _schemaRegistry.GetSchema(gridResultSchemaTypeReference);
            if (!(schemaDefinition is ObjectSchema objectSchema))
            {
                throw new InvalidOperationException($"Unexpected grid result type: {schemaDefinition.GetType()}");
            }

            if (!schemaPropertyMap.TryGetValue(objectSchema, out ObjectContractDefinition objectContractDefinition))
                return (null, null, null);

            IDictionary<string, ObjectSchemaProperty> propertyMap = objectSchema.Properties.ToDictionary(x => x.Name.Value);

            return (objectSchema, propertyMap, objectContractDefinition.Properties);
        }

        private static string BuildKey(TypeReference type)
        {
            switch (type)
            {
                case PrimitiveTypeReference primitiveTypeReference: return $"{typeof(PrimitiveType)}.{primitiveTypeReference.Type}";
                case SchemaTypeReference schemaTypeReference: return schemaTypeReference.Key;
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private readonly struct ObjectContractDefinition
        {
            public ObjectSchema Schema { get; }
            public ICollection<ObjectSchemaProperty> Properties { get; }

            public ObjectContractDefinition(ObjectSchema schema)
            {
                Schema = schema;
                Properties = Clone(schema.Properties);
            }

            private static ICollection<ObjectSchemaProperty> Clone(IEnumerable<ObjectSchemaProperty> properties)
            {
                ICollection<ObjectSchemaProperty> collection = new Collection<ObjectSchemaProperty>();
                collection.AddRange(properties.Where(x => x.Type != null));
                return collection;
            }
        }

        private readonly struct UserDefinedTypeParameter
        {
            public string Name { get; }
            public UserDefinedTypeSchema Schema { get; }

            public UserDefinedTypeParameter(string name, UserDefinedTypeSchema schema)
            {
                Name = name;
                Schema = schema;
            }
        }

        private sealed class ValidationSchemaVisitor : SchemaVisitor
        {
            private readonly IDictionary<ObjectSchema, ObjectContractDefinition> _schemaPropertyMap;
            private readonly Action<ICollection<ObjectSchemaProperty>, ObjectSchemaProperty> _propertyVisitor;

            public ValidationSchemaVisitor(ISchemaRegistry schemaRegistry, IDictionary<ObjectSchema, ObjectContractDefinition> schemaPropertyMap, Action<ICollection<ObjectSchemaProperty>, ObjectSchemaProperty> propertyVisitor) : base(schemaRegistry)
            {
                _schemaPropertyMap = schemaPropertyMap;
                _propertyVisitor = propertyVisitor;
            }

            protected override void Visit(ObjectSchema node)
            {
                if (!_schemaPropertyMap.TryGetValue(node, out ObjectContractDefinition objectContractDefinition)) 
                    return;

                foreach (ObjectSchemaProperty property in node.Properties)
                {
                    _propertyVisitor(objectContractDefinition.Properties, property);
                }
            }
        }
    }
}
