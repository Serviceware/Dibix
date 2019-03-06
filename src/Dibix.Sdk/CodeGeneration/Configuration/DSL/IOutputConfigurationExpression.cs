namespace Dibix.Sdk.CodeGeneration
{
    public interface IOutputConfigurationExpression
    {
        IOutputConfigurationExpression Formatting(CommandTextFormatting formatting);
        IOutputConfigurationExpression Namespace(string @namespace);
        IOutputConfigurationExpression ClassName(string className);
    }
}