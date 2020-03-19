using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.VisualStudio
{
    public interface IOutputConfigurationExpression
    {
        IOutputConfigurationExpression Formatting(CommandTextFormatting formatting);
        IOutputConfigurationExpression Namespace(string @namespace);
        IOutputConfigurationExpression ClassName(string className);
    }
}