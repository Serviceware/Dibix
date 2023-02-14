using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class UserDefinedTypeParameterModelValidator : ICodeGenerationModelValidator
    {
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;

        public UserDefinedTypeParameterModelValidator(ISchemaRegistry schemaRegistry, ILogger logger)
        {
            _schemaRegistry = schemaRegistry;
            _logger = logger;
        }

        public bool Validate(CodeGenerationModel model)
        {
            bool result = true;
            foreach (ControllerDefinition controller in model.Controllers)
            {
                foreach (ActionDefinition action in controller.Actions)
                {
                    foreach (ActionParameter parameter in action.Parameters)
                    {
                        if (!ValidateParameter(parameter, action))
                            result = false;
                    }
                }
            }
            return result;
        }

        private bool ValidateParameter(ActionParameter parameter, ActionDefinition action)
        {
            if (!(parameter.Type is SchemaTypeReference parameterSchemaTypeReference))
                return true;

            if (_schemaRegistry.GetSchema(parameterSchemaTypeReference) is not UserDefinedTypeSchema userDefinedTypeSchema)
                return true;

            if (userDefinedTypeSchema.Properties.Count <= 1)
                return true;

            if (parameter.ParameterSource is ActionParameterBodySource { ConverterName: { } })
                return true;

            if (action.RequestBody == null)
                return true;

            TypeReference bodyContract = action.RequestBody.Contract;
            if (bodyContract == null) // Already logged at 'TypeResolverFacade.ResolveType'
                return false;

            if (!(bodyContract is SchemaTypeReference bodySchemaTypeReference))
            {
                _logger.LogError($"Unexpected request body contract '{bodyContract}'. Expected object schema when mapping complex UDT parameter: @{parameter.InternalParameterName} {userDefinedTypeSchema.UdtName}.", bodyContract.Location.Source, bodyContract.Location.Line, bodyContract.Location.Column);
                return false;
            }

            SchemaDefinition bodySchema = _schemaRegistry.GetSchema(bodySchemaTypeReference);
            if (bodySchema is not ObjectSchema bodyObjectSchema)
            {
                _logger.LogError($"Unexpected request body contract '{bodySchema}'. Expected object schema when mapping complex UDT parameter: @{parameter.InternalParameterName} {userDefinedTypeSchema.UdtName}.", bodyContract.Location.Source, bodyContract.Location.Line, bodyContract.Location.Column);
                return false;
            }

            ObjectSchemaProperty sourceProperty = bodyObjectSchema.Properties.SingleOrDefault(x => String.Equals(x.Name, parameter.InternalParameterName, StringComparison.OrdinalIgnoreCase));
            ActionParameterPropertySource propertySource = parameter.ParameterSource as ActionParameterPropertySource;
            if (sourceProperty == null && propertySource != null) 
                sourceProperty = bodyObjectSchema.Properties.SingleOrDefault(x => String.Equals(x.Name, propertySource.PropertyName, StringComparison.OrdinalIgnoreCase));

            ActionTarget target = action.Target;
            if (sourceProperty == null)
            {
                _logger.LogError($"Target parameter '@{parameter.InternalParameterName}' can not be mapped from body contract '{bodySchemaTypeReference.Key}' and no explicit parameter override exists", target.SourceLocation.Source, target.SourceLocation.Line, target.SourceLocation.Column);
                return false;
            }

            if (!(sourceProperty.Type is SchemaTypeReference sourcePropertySchemaTypeReference))
            {
                _logger.LogError($"Unexpected contract '{sourceProperty.Type?.GetType()}' for source property '{bodySchemaTypeReference.Key}.{sourceProperty.Name.Value}'. Expected object schema when mapping complex UDT parameter: @{parameter.InternalParameterName} {userDefinedTypeSchema.UdtName}.", target.SourceLocation.Source, target.SourceLocation.Line, target.SourceLocation.Column);
                return false;
            }

            SchemaDefinition sourcePropertySchema = _schemaRegistry.GetSchema(sourcePropertySchemaTypeReference);
            if (sourcePropertySchema == null) // Already logged at 'SchemaDefinitionResolver.Resolve'
                return false;

            if (!(sourcePropertySchema is ObjectSchema sourcePropertyObjectSchema))
            {
                _logger.LogError($"Unexpected contract '{sourcePropertySchema?.GetType()}' for source property '{bodySchemaTypeReference.Key}.{sourceProperty.Name.Value}'. Expected object schema when mapping complex UDT parameter: @{parameter.InternalParameterName} {userDefinedTypeSchema.UdtName}.", target.SourceLocation.Source, target.SourceLocation.Line, target.SourceLocation.Column);
                return false;
            }

            HashSet<string> sourceProperties = new HashSet<string>(sourcePropertyObjectSchema.Properties.Select(x => x.Name.Value), StringComparer.OrdinalIgnoreCase);
            if (propertySource != null)
            {
                sourceProperties.AddRange(propertySource.ItemSources.Select(x => x.ParameterName));
            }

            bool result = true;
            foreach (ObjectSchemaProperty targetProperty in userDefinedTypeSchema.Properties)
            {
                string targetPropertyName = targetProperty.Name;
                if (sourceProperties.Contains(targetPropertyName))
                    continue;

                _logger.LogError($"UDT column '{userDefinedTypeSchema.UdtName}.[{targetPropertyName}]' can not be mapped. Create a property on source contract '{sourcePropertySchemaTypeReference.Key}' of the same name or create an explicit parameter mapping on the endpoint action.", target.SourceLocation.Source, target.SourceLocation.Line, target.SourceLocation.Column);
                result = false;
            }

            return result;
        }
    }
}