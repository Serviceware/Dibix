using System;
using System.Net.Http;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class CodeGenerationContextExtensions
    {
        public static void AddDibixHttpServerReference(this CodeGenerationContext context)
        {
            context.AddUsing("Dibix.Http.Server");
            context.Model.AdditionalAssemblyReferences.Add("Dibix.Http.Server.dll");
        }

        public static void AddDibixHttpClientReference(this CodeGenerationContext context)
        {
            context.AddUsing("Dibix.Http.Client");
            context.Model.AdditionalAssemblyReferences.Add("Dibix.Http.Client.dll");
        }

        public static void AddHttpClientReference(this CodeGenerationContext context)
        {
            Type type = typeof(HttpClient);
            context.AddUsing(type.Namespace);
            context.Model.AdditionalAssemblyReferences.Add($"{type.Assembly.GetName().Name}.dll");
        }

        public static void AddHttpFormattingReference(this CodeGenerationContext context)
        {
            Type type = typeof(HttpContentExtensions);
            context.AddUsing(type.Namespace);
            context.Model.AdditionalAssemblyReferences.Add($"{type.Assembly.GetName().Name}.dll");
        }
    }
}