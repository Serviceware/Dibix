using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IGeneratorConfigurationBuilderExpression
    {
        IGeneratorConfigurationBuilderExpression AddSource(string projectName);
        //IDaoConfigurationBuilderExpression AddSource(Action<IPhysicalSourceSelectionExpression> configuration);
        IGeneratorConfigurationBuilderExpression AddSource(string projectName, Action<IPhysicalSourceSelectionExpression> configuration);
        IGeneratorConfigurationBuilderExpression AddDacPac(string packagePath, Action<IDacPacSelectionExpression> configuration);
        IGeneratorConfigurationBuilderExpression SelectOutputWriter<TWriter>() where TWriter : IWriter;
        IGeneratorConfigurationBuilderExpression SelectOutputWriter<TWriter>(Action<IOutputConfigurationExpression> configuration) where TWriter : IWriter;
    }
}