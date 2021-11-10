using System;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractArtifactValidator : ICodeArtifactsGenerationModelValidator
    {
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;

        public ContractArtifactValidator(ISchemaRegistry schemaRegistry, ILogger logger)
        {
            this._logger = logger;
            this._schemaRegistry = schemaRegistry;
        }

        public bool Validate(CodeGenerationModel model)
        {
            bool isValid = true;
            foreach (ContractDefinition contractDefinition in model.Contracts)
            {
                // Validate unused contracts
                string contractFullName = contractDefinition.Schema.FullName;
                if (!contractDefinition.IsUsed)
                {
                    isValid = false;
                    this._logger.LogError(null, $"Unused contract definition: {contractFullName}", contractDefinition.FilePath, contractDefinition.Line, contractDefinition.Column);
                }

                // Validate enum members
                if (contractDefinition.Schema is ObjectSchema objectSchema)
                {
                    foreach (ObjectSchemaProperty property in objectSchema.Properties)
                    {
                        DefaultValue defaultValue = property.DefaultValue;
                        if (!this.IsEnumDefaultValue(defaultValue, property, out EnumSchema enumSchema))
                            continue;

                        if (this.TryCollectEnumDefault(defaultValue, enumSchema, out EnumSchemaMember defaultEnumMember))
                            defaultValue.EnumMember = defaultEnumMember;
                        else
                            isValid = false;
                    }
                }
            }

            return isValid;
        }

        private bool TryCollectEnumDefault(DefaultValue defaultValue, EnumSchema enumSchema, out EnumSchemaMember defaultEnumMember)
        {
            if (defaultValue.Value is string enumMemberName)
            {
                EnumSchemaMember enumMember = enumSchema.Members.FirstOrDefault(x => x.Name == enumMemberName);
                if (enumMember != null)
                {
                    defaultEnumMember = enumMember;
                    return true;
                }

                this._logger.LogError(code: null, $"Enum '{enumSchema.FullName}' does not define a member named '{enumMemberName}'", defaultValue.Source, defaultValue.Line, defaultValue.Column);
                defaultEnumMember = null;
                return false;
            }
            else
            {
                EnumSchemaMember enumMember = null;
                if (defaultValue.Value is byte || defaultValue.Value is short || defaultValue.Value is int || defaultValue.Value is long)
                    enumMember = enumSchema.Members.FirstOrDefault(x => Equals(x.ActualValue, Convert.ToInt32(defaultValue.Value)));

                if (enumMember != null)
                {
                    defaultEnumMember = enumMember;
                    return true;
                }

                this._logger.LogError(code: null, $"Enum '{enumSchema.FullName}' does not define a member with value '{defaultValue.Value}'", defaultValue.Source, defaultValue.Line, defaultValue.Column);
                defaultEnumMember = null;
                return true;
            }
        }

        private bool IsEnumDefaultValue(DefaultValue defaultValue, ObjectSchemaProperty property, out EnumSchema enumSchema)
        {
            if (defaultValue != null
                && property.Type is SchemaTypeReference schemaTypeReference
                && this._schemaRegistry.GetSchema(schemaTypeReference) is EnumSchema enumSchemaReference)
            {
                enumSchema = enumSchemaReference;
                return true;
            }

            enumSchema = null;
            return false;
        }
    }
}