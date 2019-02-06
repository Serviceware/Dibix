using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlAccessorGenerator
    {
        string Generate();
        ISqlAccessorGenerator AddSource(string projectName);
        //ISqlAccessorGenerator AddSource(Action<IPhysicalSourceSelectionExpression> configuration);
        ISqlAccessorGenerator AddSource(string projectName, Action<IPhysicalSourceSelectionExpression> configuration);
        ISqlAccessorGenerator AddDacPac(string packagePath, Action<IDacPacSelectionExpression> configuration);
        ISqlAccessorGenerator SelectOutputWriter<TWriter>() where TWriter : IWriter, new();
        ISqlAccessorGenerator SelectOutputWriter<TWriter>(Action<IOutputConfigurationExpression> configuration) where TWriter : IWriter, new();
    }
}