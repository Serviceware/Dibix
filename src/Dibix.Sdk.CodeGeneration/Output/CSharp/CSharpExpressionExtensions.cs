namespace Dibix.Sdk.CodeGeneration.CSharp
{
    internal static class CSharpExpressionExtensions
    {
        public static string AsString(this CSharpExpression expression)
        {
            StringWriter writer = new StringWriter();
            expression.Write(writer);
            return writer.ToString();
        }
    }
}