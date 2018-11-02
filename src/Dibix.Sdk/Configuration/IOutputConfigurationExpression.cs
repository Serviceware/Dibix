namespace Dibix.Sdk
{
    public interface IOutputConfigurationExpression
    {
        IOutputConfigurationExpression Formatting(SqlQueryOutputFormatting formatting);
        IOutputConfigurationExpression Namespace(string @namespace);
        IOutputConfigurationExpression ClassName(string className);
    }
}