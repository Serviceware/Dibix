using System;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterItemSourceBuilder
    {
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;

        public string ParameterName { get; }
        public ActionParameterSourceBuilder SourceBuilder { get; }
        public SourceLocation Location { get; }

        public ActionParameterItemSourceBuilder(string parameterName, ActionParameterSourceBuilder sourceBuilder, SourceLocation sourceLocation, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            _schemaRegistry = schemaRegistry;
            _logger = logger;
            ParameterName = parameterName;
            SourceBuilder = sourceBuilder;
            Location = sourceLocation;
        }

        public ActionParameterItemSource Build(TypeReference type)
        {
            TypeReference currentType = type;
            if (!type.IsUserDefinedType(_schemaRegistry, out UserDefinedTypeSchema userDefinedTypeSchema))
            {
                _logger.LogError($"Unexpected parameter type '{type?.GetType()}'. The ITEM property source can only be used to map to an UDT parameter.", Location.Source, Location.Line, Location.Column);
            }
            else
            {
                ObjectSchemaProperty udtColumn = userDefinedTypeSchema.Properties.FirstOrDefault(x => String.Equals(ParameterName, x.Name, StringComparison.OrdinalIgnoreCase));
                if (udtColumn != null)
                {
                    currentType = udtColumn.Type;
                }
                else
                {
                    this._logger.LogError($"Could not find column '{ParameterName}' on UDT '{userDefinedTypeSchema.UdtName}'", Location.Source, Location.Line, Location.Column);
                }
            }

            ActionParameterSource source = SourceBuilder.Build(currentType);
            ActionParameterItemSource itemSource = new ActionParameterItemSource(ParameterName, source, Location);
            return itemSource;
        }
    }
}