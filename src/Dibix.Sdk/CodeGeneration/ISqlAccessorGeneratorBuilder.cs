using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlAccessorGeneratorBuilder
    {
        ISqlAccessorGeneratorBuilder AddSource(string projectName);
        //ISqlAccessorGeneratorBuilder AddSource(Action<IPhysicalSourceSelectionExpression> configuration);
        ISqlAccessorGeneratorBuilder AddSource(string projectName, Action<IPhysicalSourceSelectionExpression> configuration);
        ISqlAccessorGeneratorBuilder AddDacPac(string packagePath, Action<IDacPacSelectionExpression> configuration);
        ISqlAccessorGeneratorBuilder SelectOutputWriter<TWriter>() where TWriter : IWriter, new();
        ISqlAccessorGeneratorBuilder SelectOutputWriter<TWriter>(Action<IOutputConfigurationExpression> configuration) where TWriter : IWriter, new();
        string Generate();
    }
}