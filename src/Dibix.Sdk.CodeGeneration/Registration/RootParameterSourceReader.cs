using System.Collections.Generic;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class RootParameterSourceReader(ISchemaRegistry schemaRegistry, ILogger logger, IActionParameterSourceRegistry actionParameterSourceRegistry, IActionParameterConverterRegistry actionParameterConverterRegistry)
        : ParameterSourceReaderBase(CollectReaders(schemaRegistry, logger, actionParameterSourceRegistry, actionParameterConverterRegistry))
    {
        private static IEnumerable<IParameterSourceReader> CollectReaders(ISchemaRegistry schemaRegistry, ILogger logger, IActionParameterSourceRegistry actionParameterSourceRegistry, IActionParameterConverterRegistry actionParameterConverterRegistry)
        {
            yield return new ConstantParameterSourceReader(schemaRegistry, logger);
            yield return new PropertyPathParameterSourceReader(schemaRegistry, logger, actionParameterSourceRegistry);
            yield return new ItemsParameterSourceReader(schemaRegistry, logger, actionParameterSourceRegistry, actionParameterConverterRegistry);
            yield return new ParameterSourceDescriptorReader(schemaRegistry, logger, actionParameterSourceRegistry, actionParameterConverterRegistry);
            yield return new BodyConverterParameterSourceReader();
        }
    }
}