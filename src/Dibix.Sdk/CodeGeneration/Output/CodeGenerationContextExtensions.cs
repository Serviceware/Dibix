namespace Dibix.Sdk.CodeGeneration
{
    internal static class CodeGenerationContextExtensions
    {
        public static void AddDibixHttpServerReference(this CodeGenerationContext context)
        {
            context.AddUsing("Dibix.Http.Server");
            context.Model.AdditionalAssemblyReferences.Add("Dibix.Http.Server.dll");
        }
    }
}