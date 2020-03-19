using System;

namespace Dibix.Sdk.VisualStudio
{
    public interface ICodeGeneratorConfigurationExpression
    {
        ICodeGeneratorConfigurationExpression AddSource(string projectName, Action<IPhysicalSourceSelectionExpression> configuration);
        ICodeGeneratorConfigurationExpression AddDacPac(string packagePath, Action<IDacPacSelectionExpression> configuration);
        ICodeGeneratorConfigurationExpression SelectOutputWriter<TWriter>(Action<IOutputConfigurationExpression> configuration);
    }
}