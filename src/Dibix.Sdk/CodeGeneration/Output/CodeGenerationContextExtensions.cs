using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class CodeGenerationContextExtensions
    {
        public static CodeGenerationContext AddDibixHttpServerReference(this CodeGenerationContext context)
        {
            context.AddUsing("Dibix.Http.Server");
            context.Model.AdditionalAssemblyReferences.Add("Dibix.Http.Server.dll");
            return context;
        }

        public static CodeGenerationContext AddDibixHttpClientReference(this CodeGenerationContext context)
        {
            context.AddUsing("Dibix.Http.Client");
            context.Model.AdditionalAssemblyReferences.Add("Dibix.Http.Client.dll");
            return context;
        }

        public static CodeGenerationContext AddReference<TType>(this CodeGenerationContext context)
        {
            Type type = typeof(TType);
            context.AddUsing<TType>();
            context.Model.AdditionalAssemblyReferences.Add($"{type.Assembly.GetName().Name}.dll");
            return context;
        }

        public static CodeGenerationContext AddUsing<TType>(this CodeGenerationContext context)
        {
            Type type = typeof(TType);
            context.AddUsing(type.Namespace);
            return context;
        }
    }
}