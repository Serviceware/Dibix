using System;
using Dibix.Sdk.CodeGeneration;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.VisualStudio
{
    public static class TextTemplateCodeGenerator
    {
        public static string Generate(ITextTemplatingEngineHost textTemplatingEngineHost, IServiceProvider serviceProvider, Action<ICodeGeneratorConfigurationExpression> configure)
        {
            Guard.IsNotNull(textTemplatingEngineHost, nameof(textTemplatingEngineHost));
            Guard.IsNotNull(serviceProvider, nameof(serviceProvider));
            Guard.IsNotNull(configure, nameof(configure));

            ILogger logger = new TextTemplatingEngineLogger(textTemplatingEngineHost);
            ISchemaRegistry schemaRegistry = new SchemaRegistry(logger);
            CodeGenerationModel model = TextTemplateCodeGenerationModelLoader.Create(textTemplatingEngineHost, serviceProvider, schemaRegistry, logger, configure);
            CodeGenerator generator = new ServerCodeGenerator(logger, schemaRegistry);
            return generator.Generate(model);
        }
    }
}