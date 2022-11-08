using System;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterItemSourceBuilder
    {
        private readonly ISchemaDefinitionResolver _schemaDefinitionResolver;
        private readonly ILogger _logger;

        public string ParameterName { get; }
        public ActionParameterSourceBuilder SourceBuilder { get; }
        public string FilePath { get; }
        public int Line { get; }
        public int Column { get; }

        public ActionParameterItemSourceBuilder(string parameterName, ActionParameterSourceBuilder sourceBuilder, string filePath, int line, int column, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger)
        {
            ParameterName = parameterName;
            SourceBuilder = sourceBuilder;
            FilePath = filePath;
            Line = line;
            Column = column;
            _schemaDefinitionResolver = schemaDefinitionResolver;
            _logger = logger;
        }

        public ActionParameterItemSource Build(TypeReference type)
        {
            TypeReference currentType = type;
            if (!type.IsUserDefinedType(_schemaDefinitionResolver, out UserDefinedTypeSchema userDefinedTypeSchema))
            {
                _logger.LogError($"Unexpected parameter type '{type?.GetType()}'. The ITEM property source can only be used to map to an UDT parameter.", FilePath, Line, Column);
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
                    this._logger.LogError($"Could not find column '{ParameterName}' on UDT '{userDefinedTypeSchema.UdtName}'", FilePath, Line, Column);
                }
            }

            ActionParameterSource source = SourceBuilder.Build(currentType);
            ActionParameterItemSource itemSource = new ActionParameterItemSource(ParameterName, source, FilePath, Line, Column);
            return itemSource;
        }
    }
}