using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ICodeGeneratorConfigurationExpression
    {
        ICodeGeneratorConfigurationExpression AddSource(string projectName, Action<IPhysicalSourceSelectionExpression> configuration);
        ICodeGeneratorConfigurationExpression AddDacPac(string packagePath, Action<IDacPacSelectionExpression> configuration);
        ICodeGeneratorConfigurationExpression SelectOutputWriter<TWriter>(Action<IOutputConfigurationExpression> configuration);
    }
}