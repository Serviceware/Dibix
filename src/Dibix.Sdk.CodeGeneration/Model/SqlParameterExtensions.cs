namespace Dibix.Sdk.CodeGeneration
{
    internal static class SqlParameterExtensions
    {
        public static bool HasParameterOptions(this SqlQueryParameter parameter) => parameter.Obfuscate || parameter.IsOutput || parameter.Type.GetStringSize() != null;
    }
}