namespace Dibix.Sdk.CodeGeneration
{
    internal static class DaoCodeGenerationContextExtensions
    {
        public static void AddDibixHttpReference(this DaoCodeGenerationContext context)
        {
            context.AddUsing("Dibix.Http");
            context.Model.AdditionalAssemblyReferences.Add("Dibix.Http.dll");
        }
    }
}