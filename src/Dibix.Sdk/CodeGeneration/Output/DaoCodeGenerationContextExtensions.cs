namespace Dibix.Sdk.CodeGeneration
{
    internal static class DaoCodeGenerationContextExtensions
    {
        public static void AddDibixHttpServerReference(this DaoCodeGenerationContext context)
        {
            context.AddUsing("Dibix.Http.Server");
            context.Model.AdditionalAssemblyReferences.Add("Dibix.Http.Server.dll");
        }
    }
}