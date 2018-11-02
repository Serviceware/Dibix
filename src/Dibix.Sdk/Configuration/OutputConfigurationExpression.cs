using System;

namespace Dibix.Sdk
{
    internal class OutputConfigurationExpression : IOutputConfigurationExpression
    {
        #region Fields
        private readonly IExecutionEnvironment _environment;
        private readonly IWriter _writer;
        #endregion

        #region Constructor
        public OutputConfigurationExpression(IExecutionEnvironment environment, IWriter writer)
        {
            this._environment = environment;
            this._writer = writer;
        }
        #endregion

        #region IOutputConfigurationExpression Members
        public IOutputConfigurationExpression Formatting(SqlQueryOutputFormatting formatting)
        {
            this._writer.Formatting = formatting;
            return this;
        }

        public IOutputConfigurationExpression Namespace(string @namespace)
        {
            this._writer.Namespace = @namespace;
            return this;
        }

        public IOutputConfigurationExpression ClassName(string className)
        {
            this._writer.ClassName = className;
            return this;
        }
        #endregion

        #region Internal Methods
        internal void Build()
        {
            // Detect namespace and class name
            if (String.IsNullOrEmpty(this._writer.Namespace))
                this._writer.Namespace = this._environment.GetProjectDefaultNamespace();

            if (String.IsNullOrEmpty(this._writer.ClassName))
                this._writer.ClassName = this._environment.GetClassName();
        }
        #endregion
    }
}