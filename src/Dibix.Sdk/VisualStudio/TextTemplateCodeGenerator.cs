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

            IErrorReporter errorReporter = new TextTemplatingEngineErrorReporter(textTemplatingEngineHost);
            ISchemaRegistry schemaRegistry = new SchemaRegistry(errorReporter);
            CodeGenerationModel model = TextTemplateCodeGenerationModelLoader.Create(textTemplatingEngineHost, serviceProvider, schemaRegistry, errorReporter, configure);
            CodeGenerator generator = new DaoCodeGenerator(errorReporter, schemaRegistry);
            return generator.Generate(model);
        }
    }
}