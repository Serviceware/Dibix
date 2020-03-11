using System;
using System.Linq;
using System.Reflection;
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
            CodeGenerationModel model;
            try
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += OnReflectionOnlyAssemblyResolve;
                model = TextTemplateCodeGenerationModelLoader.Create(textTemplatingEngineHost, serviceProvider, schemaRegistry, errorReporter, configure);
            }
            finally
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= OnReflectionOnlyAssemblyResolve;
            }
            CodeGenerator generator = new DaoCodeGenerator(errorReporter, schemaRegistry);
            return generator.Generate(model);
        }

        private static Assembly OnReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName == args.Name);
            return Assembly.ReflectionOnlyLoadFrom(assembly.Location) ?? Assembly.ReflectionOnlyLoad(args.Name);
        }
    }
}