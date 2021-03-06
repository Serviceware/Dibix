using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractArtifactModelValidator : ICodeGenerationModelValidator
    {
        private readonly ISchemaDefinitionResolver _schemaDefinitionResolver;
        private readonly ILogger _logger;

        public ContractArtifactModelValidator(ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger)
        {
            this._schemaDefinitionResolver = schemaDefinitionResolver;
            this._logger = logger;
        }

        public bool Validate(CodeGenerationModel model)
        {
            bool isValid = this.ValidateUnusedContracts(model)
                        && this.ValidateUnusedContractProperties(model);
            return isValid;
        }

        private bool ValidateUnusedContracts(CodeGenerationModel model)
        {
            bool isValid = true;
            foreach (ContractDefinition contractDefinition in model.Contracts)
            {
                // Validate unused contracts
                string contractFullName = contractDefinition.Schema.FullName;
                if (!contractDefinition.HasReferences)
                {
                    isValid = false;
                    this._logger.LogError($"Unused contract definition: {contractFullName}", contractDefinition.FilePath, contractDefinition.Line, contractDefinition.Column);
                }
            }
            return isValid;
        }

        private bool ValidateUnusedContractProperties(CodeGenerationModel model)
        {
            IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap = model.Contracts
                                                                                                  .Select(x => x.Schema)
                                                                                                  .OfType<ObjectSchema>()
                                                                                                  .ToDictionary(x => x, x => Clone(x.Properties));

            this.ValidateStatementContracts(model, schemaPropertyMap);
            this.ValidateEndpointContracts(model, schemaPropertyMap);

            bool isValid = true;
            foreach (KeyValuePair<ObjectSchema, ICollection<ObjectSchemaProperty>> keyValuePair in schemaPropertyMap)
            {
                ObjectSchema objectSchema = keyValuePair.Key;
                foreach (ObjectSchemaProperty property in keyValuePair.Value)
                {
                    isValid = false;
                    Token<string> name = property.Name;
                    this._logger.LogError($"Unused contract definition property: {objectSchema.FullName}.{name.Value}", name.Source, name.Line, name.Column);
                }
            }
            
            return isValid;
        }

        private void ValidateStatementContracts(IPersistedCodeGenerationModel model, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap)
        {
            foreach (SqlStatementDefinition statement in model.SqlStatements)
            {
                (ObjectSchema gridResultSchema, IDictionary<string, ObjectSchemaProperty> gridResultSchemaPropertyMap, ICollection<ObjectSchemaProperty> gridResultSchemaProperties) = this.CollectGridResultInfos(statement, schemaPropertyMap);

                for (int i = 0; i < statement.Results.Count; i++)
                {
                    SqlQueryResult result = statement.Results[i];
                    this.ValidateResultSchemaProperties(result, schemaPropertyMap);
                    ValidateGridResultSchemaProperties(statement, result, i, gridResultSchema, gridResultSchemaPropertyMap, gridResultSchemaProperties);
                }
            }
        }

        private void ValidateResultSchemaProperties(SqlQueryResult result, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap)
        {
            // These types will automatically be mapped on an existing property of the same type
            ICollection<string> multiMapTypes = new HashSet<string>(result.Types.Select(BuildKey));

            foreach (TypeReference type in result.Types)
            {
                this.ValidateResultSchemaProperties(result, type, multiMapTypes, schemaPropertyMap);
            }

            if (result.ProjectToType != null)
            {
                this.ValidateResultSchemaProperties(result, result.ProjectToType, multiMapTypes, schemaPropertyMap);
            }
        }

        private void ValidateResultSchemaProperties(SqlQueryResult result, TypeReference type, ICollection<string> multiMapTypes, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap)
        {
            if (!(type is SchemaTypeReference resultSchemaTypeReference))
                return;

            if (!(this._schemaDefinitionResolver.Resolve(resultSchemaTypeReference) is ObjectSchema objectSchema))
                return;

            // Some properties in Dibix.FileEntity for example are optional
            if (objectSchema.Source == SchemaDefinitionSource.Internal)
                return;

            if (!schemaPropertyMap.TryGetValue(objectSchema, out ICollection<ObjectSchemaProperty> properties))
                return;

            foreach (ObjectSchemaProperty property in objectSchema.Properties)
            {
                if (!IsPropertyUsed(result, property, multiMapTypes))
                    continue;

                VisitProperty(properties, property);
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

        private void ValidateEndpointContracts(CodeGenerationModel model, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap)
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

        private void ValidateBodyContract(ActionDefinition action, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap)
        {
            if (!(action.RequestBody?.Contract is SchemaTypeReference schemaTypeReference))
                return;

            if (!(this._schemaDefinitionResolver.Resolve(schemaTypeReference) is ObjectSchema objectSchema))
                return;

            if (!schemaPropertyMap.TryGetValue(objectSchema, out ICollection<ObjectSchemaProperty> properties))
                return;

            switch (action.Target)
            {
                case LocalActionTarget localActionTarget:
                    this.VisitLocalActionTarget(action, localActionTarget, objectSchema, properties, schemaPropertyMap);
                    break;

                case ReflectionActionTarget _:
                    this.VisitReflectionActionTarget(objectSchema, schemaPropertyMap);
                    break;
            }
        }

        private void ValidateResponseContracts(ActionDefinition action, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap)
        {
            foreach (ActionResponse response in action.Responses.Values)
            {
                if (response.ResultType == null)
                    continue;

                VisitAllPropertiesNested(response.ResultType, schemaPropertyMap);
            }
        }

        private void VisitLocalActionTarget(ActionDefinition action, LocalActionTarget localActionTarget, ObjectSchema bodySchema, ICollection<ObjectSchemaProperty> bodyProperties, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap)
        {
            IDictionary<string, UserDefinedTypeParameter?> parameters = localActionTarget.SqlStatementDefinition.Parameters.ToDictionary(x => x.Name, this.CollectUDTParameter, StringComparer.OrdinalIgnoreCase);
            foreach (ActionParameter actionParameter in action.Parameters)
            {
                if (parameters.TryGetValue(actionParameter.InternalParameterName, out UserDefinedTypeParameter? userDefinedTypeParameter))
                {
                    parameters.Remove(actionParameter.InternalParameterName);

                    // The ApiParameterName might be the same for multiple target parameters, when mapping from a complex object
                    if (!parameters.ContainsKey(actionParameter.ApiParameterName))
                        parameters.Add(actionParameter.ApiParameterName, userDefinedTypeParameter);
                }

                this.VisitParameterSource(actionParameter.Source, bodySchema, bodyProperties, schemaPropertyMap);
            }

            // Visit target parameters that are mapped from body contract properties
            foreach (ObjectSchemaProperty property in bodySchema.Properties)
            {
                bool hasMatchingParameter = parameters.TryGetValue(property.Name, out UserDefinedTypeParameter? userDefinedTypeParameter);
                if (!hasMatchingParameter)
                    continue;

                VisitProperty(bodyProperties, property);
                this.VisitUDTParameter(userDefinedTypeParameter, bodySchema, property, schemaPropertyMap);
            }
        }

        private void VisitReflectionActionTarget(SchemaDefinition bodySchema, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap)
        {
            // Visit all properties of the body, if the target is not visible to us
            this.VisitAllPropertiesNested(bodySchema, schemaPropertyMap);
        }

        private UserDefinedTypeParameter? CollectUDTParameter(SqlQueryParameter parameter)
        {
            if (parameter.Type is SchemaTypeReference schemaTypeReference && this._schemaDefinitionResolver.Resolve(schemaTypeReference) is UserDefinedTypeSchema userDefinedTypeSchema)
                return new UserDefinedTypeParameter(parameter.Name, userDefinedTypeSchema);

            return null;
        }

        private void VisitParameterSource(ActionParameterSource parameterSource, ObjectSchema bodySchema, ICollection<ObjectSchemaProperty> bodyProperties, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap)
        {
            switch (parameterSource)
            {
                case ActionParameterBodySource bodySource:
                    this.VisitBodySource(bodySource, bodySchema, schemaPropertyMap);
                    break;

                case ActionParameterPropertySource propertySource:
                    this.VisitPropertySource(propertySource, bodySchema, bodyProperties, schemaPropertyMap);
                    break;
            }
        }

        private void VisitBodySource(ActionParameterBodySource bodySource, SchemaDefinition bodySchema, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap)
        {
            if (bodySource.ConverterName == null)
                return;

            // Visit all properties of the body, if they are used within a converter, that is not visible to us
            this.VisitAllPropertiesNested(bodySchema, schemaPropertyMap);
        }

        private void VisitPropertySource(ActionParameterPropertySource propertySource, ObjectSchema bodySchema, ICollection<ObjectSchemaProperty> bodyProperties, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap)
        {
            foreach (ActionParameterPropertySourceNode node in propertySource.Nodes)
            {
                if (!schemaPropertyMap.TryGetValue(node.Schema, out ICollection<ObjectSchemaProperty> nodeProperties))
                    continue;

                VisitProperty(nodeProperties, node.Property);
            }

            foreach (ActionParameterItemSource itemSource in propertySource.ItemSources)
            {
                this.VisitParameterSource(itemSource.Source, bodySchema, bodyProperties, schemaPropertyMap);
            }
        }

        private void VisitUDTParameter(UserDefinedTypeParameter? userDefinedTypeParameter, SchemaDefinition bodySchema, ObjectSchemaProperty bodyProperty, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap)
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

                this._logger.LogError($"Unexpected property contract '{bodyProperty.Type?.GetType()}' for property '{bodySchema.FullName}.{bodyProperty.Name}'. Expected object schema when mapping complex UDT parameter: @{parameterName} {udtName}.", bodyProperty.Type.Source, bodyProperty.Type.Line, bodyProperty.Type.Column);
                return;
            }

            SchemaDefinition schemaDefinition = this._schemaDefinitionResolver.Resolve(schemaTypeReference);
            if (!(schemaDefinition is ObjectSchema propertySchema))
            {
                this._logger.LogError($"Unexpected property contract '{schemaDefinition?.GetType()}' for property '{bodySchema.FullName}.{bodyProperty.Name}'. Expected object schema when mapping complex UDT parameter: @{parameterName} {udtName}.", bodyProperty.Type.Source, bodyProperty.Type.Line, bodyProperty.Type.Column);
                return;
            }

            ICollection<string> udtParameters = new HashSet<string>(userDefinedTypeParameter.Value.Schema.Properties.Select(x => x.Name.Value), StringComparer.OrdinalIgnoreCase);
            if (!schemaPropertyMap.TryGetValue(propertySchema, out ICollection<ObjectSchemaProperty> properties))
                return;

            // Mark all nested source properties as used, if a matching column on the UDT exists
            foreach (ObjectSchemaProperty property in propertySchema.Properties)
            {
                if (udtParameters.Contains(property.Name))
                    VisitProperty(properties, property);
            }
        }

        private void VisitAllPropertiesNested(SchemaDefinition schema, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap) => VisitAllPropertiesNested(schema, schemaPropertyMap, (x, y) => x.Accept(y));
        private void VisitAllPropertiesNested(TypeReference type, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap) => VisitAllPropertiesNested(type, schemaPropertyMap, (x, y) => x.Accept(y));
        private void VisitAllPropertiesNested<T>(T node, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap, Action<SchemaVisitor, T> visitorHandler)
        {
            ValidationSchemaVisitor visitor = new ValidationSchemaVisitor(this._schemaDefinitionResolver, schemaPropertyMap, VisitProperty);
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

        private (ObjectSchema schema, IDictionary<string, ObjectSchemaProperty> propertyMap, ICollection<ObjectSchemaProperty> visitedProperties) CollectGridResultInfos(SqlStatementDefinition statement, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap)
        {
            if (statement.Results.Count <= 1) 
                return (null, null, null);

            if (!(statement.ResultType is SchemaTypeReference gridResultSchemaTypeReference))
            {
                throw new InvalidOperationException($"Unexpected grid result type: {statement.ResultType.GetType()}");
            }

            SchemaDefinition schemaDefinition = this._schemaDefinitionResolver.Resolve(gridResultSchemaTypeReference);
            if (!(schemaDefinition is ObjectSchema objectSchema))
            {
                throw new InvalidOperationException($"Unexpected grid result type: {schemaDefinition.GetType()}");
            }

            if (!schemaPropertyMap.TryGetValue(objectSchema, out ICollection<ObjectSchemaProperty> visitedProperties))
                return (null, null, null);

            IDictionary<string, ObjectSchemaProperty> propertyMap = objectSchema.Properties.ToDictionary(x => x.Name.Value);

            return (objectSchema, propertyMap, visitedProperties);
        }

        private static ICollection<ObjectSchemaProperty> Clone(IEnumerable<ObjectSchemaProperty> properties)
        {
            ICollection<ObjectSchemaProperty> collection = new Collection<ObjectSchemaProperty>();
            collection.AddRange(properties);
            return collection;
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

        private readonly struct UserDefinedTypeParameter
        {
            public string Name { get; }
            public UserDefinedTypeSchema Schema { get; }

            public UserDefinedTypeParameter(string name, UserDefinedTypeSchema schema)
            {
                this.Name = name;
                this.Schema = schema;
            }
        }

        private sealed class ValidationSchemaVisitor : SchemaVisitor
        {
            private readonly IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> _schemaPropertyMap;
            private readonly Action<ICollection<ObjectSchemaProperty>, ObjectSchemaProperty> _propertyVisitor;

            public ValidationSchemaVisitor(ISchemaStore schemaStore, IDictionary<ObjectSchema, ICollection<ObjectSchemaProperty>> schemaPropertyMap, Action<ICollection<ObjectSchemaProperty>, ObjectSchemaProperty> propertyVisitor) : base(schemaStore)
            {
                this._schemaPropertyMap = schemaPropertyMap;
                this._propertyVisitor = propertyVisitor;
            }

            protected override void Visit(ObjectSchema node)
            {
                if (!this._schemaPropertyMap.TryGetValue(node, out ICollection<ObjectSchemaProperty> properties)) 
                    return;

                foreach (ObjectSchemaProperty property in node.Properties)
                {
                    this._propertyVisitor(properties, property);
                }
            }
        }
    }
}
