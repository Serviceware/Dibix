using System;

namespace Dibix.Sdk
{
    public interface ISqlAccessorGenerator
    {
        string Generate();
        ISqlAccessorGenerator AddSource(string projectName);
        ISqlAccessorGenerator AddSource(Action<ISourceSelectionExpression> configuration);
        ISqlAccessorGenerator AddSource(string projectName, Action<ISourceSelectionExpression> configuration);
        ISqlAccessorGenerator SelectOutputWriter<TWriter>() where TWriter : IWriter, new();
        ISqlAccessorGenerator SelectOutputWriter<TWriter>(Action<IOutputConfigurationExpression> configuration) where TWriter : IWriter, new();
    }
}