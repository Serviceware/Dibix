using System;
using System.IO;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.CodeGeneration
{
    public static class GeneratorConfigurationBuilder
    {
        #region Fields
        private static readonly bool UseExtendedJsonReader;
        #endregion

        #region Factory Methods
        public static IGeneratorConfigurationBuilderSourceExpression Create(IFileSystemProvider fileSystemProvider, string executingFilePath, IErrorReporter errorReporter)
        {
            return new GeneratorConfigurationBuilderSourceExpression(fileSystemProvider, executingFilePath, errorReporter);
        }

        public static IGeneratorConfigurationBuilderSourceExpression FromVisualStudio(IServiceProvider serviceProvider, string executingFilePath, IErrorReporter reporter)
        {
            IFileSystemProvider fileSystemProvider = new VisualStudioFileSystemProvider(serviceProvider, executingFilePath);
            IErrorReporter errorReporter = new VisualStudioErrorReporter(serviceProvider);
            return new GeneratorConfigurationBuilderSourceExpression(fileSystemProvider, executingFilePath, errorReporter);
        }

        public static IGeneratorConfigurationBuilderSourceExpression FromTextTemplate(ITextTemplatingEngineHost textTemplatingEngineHost, IServiceProvider serviceProvider)
        {
            IFileSystemProvider fileSystemProvider = new VisualStudioFileSystemProvider(serviceProvider, textTemplatingEngineHost.TemplateFile);
            IErrorReporter errorReporter = new TextTemplatingEngineErrorReporter(textTemplatingEngineHost);
            return new GeneratorConfigurationBuilderSourceExpression(fileSystemProvider, textTemplatingEngineHost.TemplateFile, errorReporter);
        }
        #endregion

        #region Nested Types
        private class GeneratorConfigurationBuilderSourceExpression : IGeneratorConfigurationBuilderSourceExpression
        {
            #region Fields
            private readonly IFileSystemProvider _fileSystemProvider;
            private readonly string _executingFilePath;
            private readonly IErrorReporter _errorReporter;
            #endregion

            #region Constructor
            public GeneratorConfigurationBuilderSourceExpression(IFileSystemProvider fileSystemProvider, string executingFilePath, IErrorReporter errorReporter)
            {
                this._fileSystemProvider = fileSystemProvider;
                this._executingFilePath = executingFilePath;
                this._errorReporter = errorReporter;
            }
            #endregion

            #region IGeneratorConfigurationBuilderSourceExpression Members
            public GeneratorConfiguration Configure(Action<IGeneratorConfigurationBuilderExpression> configure)
            {
                return ReadConfiguration(() => new ActionGeneratorConfigurationReader(configure, this._fileSystemProvider));
            }

            public GeneratorConfiguration LoadJson()
            {
                return this.LoadJson(this._executingFilePath);
            }
            public GeneratorConfiguration LoadJson(string filePath)
            {
                if (!Path.IsPathRooted(filePath))
                    filePath = new PhysicalFileSystemProvider(this._fileSystemProvider.CurrentDirectory).GetPhysicalFilePath(null, filePath);

                return this.ReadJsonConfiguration(() => File.ReadAllText(filePath));
            }

            public GeneratorConfiguration ParseJson(string json)
            {
                return this.ReadJsonConfiguration(() => json);
            }
            #endregion

            #region Private Methods
            private GeneratorConfiguration ReadJsonConfiguration(Func<string> jsonSelector)
            {
                return ReadConfiguration(() =>
                {
                    if (UseExtendedJsonReader)
                        return new ExtendedJsonGeneratorConfigurationReader(jsonSelector(), this._fileSystemProvider, this._errorReporter);

                    return new SimpleJsonGeneratorConfigurationReader(jsonSelector(), this._fileSystemProvider, this._errorReporter);
                });
            }

            private static GeneratorConfiguration ReadConfiguration(Func<IGeneratorConfigurationReader> readerSelector)
            {
                GeneratorConfiguration configuration = new GeneratorConfiguration();
                IGeneratorConfigurationReader reader = readerSelector();
                reader.Read(configuration);
                return configuration;
            }
            #endregion
        }
        #endregion
    }
}