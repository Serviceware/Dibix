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
            CodeGenerationModel model = TextTemplateCodeGenerationModelLoader.Create(textTemplatingEngineHost, serviceProvider, errorReporter, configure);
            CodeGenerator generator = new DaoCodeGenerator(errorReporter);
            return generator.Generate(model);
        }
    }
}