﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class UserDefinedTypeParameterModelValidator : ICodeGenerationModelValidator
    {
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;

        public UserDefinedTypeParameterModelValidator(ISchemaRegistry schemaRegistry, ILogger logger)
        {
            this._schemaRegistry = schemaRegistry;
            this._logger = logger;
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
                        if (!this.ValidateParameter(parameter, action))
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

            if (!(this._schemaRegistry.GetSchema(parameterSchemaTypeReference) is UserDefinedTypeSchema userDefinedTypeSchema))
                return true;

            if (userDefinedTypeSchema.Properties.Count <= 1)
                return true;

            if (parameter.Source is ActionParameterBodySource bodySource && bodySource.ConverterName != null)
                return true;

            if (action.RequestBody == null)
                return true;

            TypeReference bodyContract = action.RequestBody.Contract;
            if (bodyContract == null) // Already logged at 'TypeResolverFacade.ResolveType'
                return false;

            if (!(bodyContract is SchemaTypeReference bodySchemaTypeReference))
            {
                this._logger.LogError(null, $"Unexpected request body contract '{bodyContract}'. Expected object schema when mapping complex UDT parameter: @{parameter.InternalParameterName} {userDefinedTypeSchema.UdtName}.", bodyContract.Source, bodyContract.Line, bodyContract.Column);
                return false;
            }

            SchemaDefinition bodySchema = this._schemaRegistry.GetSchema(bodySchemaTypeReference);
            if (!(bodySchema is ObjectSchema bodyObjectSchema))
            {
                this._logger.LogError(null, $"Unexpected request body contract '{bodySchema}'. Expected object schema when mapping complex UDT parameter: @{parameter.InternalParameterName} {userDefinedTypeSchema.UdtName}.", bodyContract.Source, bodyContract.Line, bodyContract.Column);
                return false;
            }

            ObjectSchemaProperty sourceProperty = bodyObjectSchema.Properties.SingleOrDefault(x => String.Equals(x.Name, parameter.InternalParameterName, StringComparison.OrdinalIgnoreCase));
            ActionParameterPropertySource propertySource = parameter.Source as ActionParameterPropertySource;
            if (sourceProperty == null && propertySource != null) 
                sourceProperty = bodyObjectSchema.Properties.SingleOrDefault(x => String.Equals(x.Name, propertySource.PropertyName, StringComparison.OrdinalIgnoreCase));

            ActionDefinitionTarget target = action.Target;
            if (sourceProperty == null)
            {
                this._logger.LogError(null, $"Target parameter '@{parameter.InternalParameterName}' can not be mapped from body contract '{bodySchemaTypeReference.Key}' and no explicit parameter override exists", target.Source, target.Line, target.Column);
                return false;
            }

            if (!(sourceProperty.Type is SchemaTypeReference sourcePropertySchemaTypeReference))
            {
                this._logger.LogError(null, $"Unexpected contract '{sourceProperty.Type}' for source property '{bodySchemaTypeReference.Key}.{sourceProperty.Name}'. Expected object schema when mapping complex UDT parameter: @{parameter.InternalParameterName} {userDefinedTypeSchema.UdtName}.", target.Source, target.Line, target.Column);
                return false;
            }

            SchemaDefinition sourcePropertySchema = this._schemaRegistry.GetSchema(sourcePropertySchemaTypeReference);
            if (sourcePropertySchema == null) // Already logged at 'SchemaRegistry.GetSchema'
                return false;

            if (!(sourcePropertySchema is ObjectSchema sourcePropertyObjectSchema))
            {
                this._logger.LogError(null, $"Unexpected contract '{sourcePropertySchema}' for source property '{bodySchemaTypeReference.Key}.{sourceProperty.Name}'. Expected object schema when mapping complex UDT parameter: @{parameter.InternalParameterName} {userDefinedTypeSchema.UdtName}.", target.Source, target.Line, target.Column);
                return false;
            }

            HashSet<string> sourceProperties = new HashSet<string>(sourcePropertyObjectSchema.Properties.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
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

                this._logger.LogError(null, $"UDT column '{userDefinedTypeSchema.UdtName}.[{targetPropertyName}]' can not be mapped. Create a property on source contract '{sourcePropertySchemaTypeReference.Key}' of the same name or create an explicit parameter mapping on the endpoint action.", target.Source, target.Line, target.Column);
                result = false;
            }

            return result;
        }
    }
}