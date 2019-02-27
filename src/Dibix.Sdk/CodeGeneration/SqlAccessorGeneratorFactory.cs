using System;
using System.IO;
using Microsoft.VisualStudio.TextTemplating;

namespace Dibix.Sdk.CodeGeneration
{
    public static class SqlAccessorGeneratorFactory
    {
        #region Factory Methods
        // Unit tests
        public static ISqlAccessorGeneratorFactoryExpression Create(IExecutionEnvironment environment)
        {
            return new SqlAccessorGeneratorFactoryExpression(environment);
        }

        // Visual studio
        public static ISqlAccessorGeneratorFactoryExpression FromVisualStudio(ITextTemplatingEngineHost host, IServiceProvider serviceProvider)
        {
            IExecutionEnvironment environment = new VisualStudioExecutionEnvironment(host, serviceProvider);
            return new SqlAccessorGeneratorFactoryExpression(environment);
        }
        #endregion

        #region Nested Types
        private class SqlAccessorGeneratorFactoryExpression : ISqlAccessorGeneratorFactoryExpression
        {
            #region Fields
            private readonly IExecutionEnvironment _environment;
            private readonly ISqlAccessorGeneratorConfigurationFactory _configurationFactory;
            #endregion

            #region Constructor
            public SqlAccessorGeneratorFactoryExpression(IExecutionEnvironment environment)
            {
                this._environment = environment;
                this._configurationFactory = new SqlAccessorGeneratorConfigurationFactory();
            }
            #endregion

            #region ISqlAccessorGeneratorFactoryExpression Members
            public ISqlAccessorGeneratorBuilder Build()
            {
                return new SqlAccessorGeneratorBuilder(this._environment, this._configurationFactory);
            }

            public string LoadJson(string filePath)
            {
                if (!Path.IsPathRooted(filePath))
                    filePath = Path.Combine(this._environment.GetCurrentDirectory(), filePath);

                return this.ParseJson(File.ReadAllText(filePath));
            }

            public string ParseJson(string json)
            {
                SqlAccessorGeneratorConfiguration configuration = this._configurationFactory.CreateConfiguration(this._environment);
                configuration.ApplyFromJson(json, this._environment, this._configurationFactory);
                ICodeGenerator generator = new SqlAccessorGenerator(configuration, this._environment);
                string output = generator.Generate();
                return output;
            }
            #endregion
        }
        #endregion
    }
}