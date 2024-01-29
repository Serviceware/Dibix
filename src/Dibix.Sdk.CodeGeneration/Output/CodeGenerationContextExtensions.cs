using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class CodeGenerationContextExtensions
    {
        public static CodeGenerationContext AddUsing<TType>(this CodeGenerationContext context) => AddUsing(context, typeof(TType));
        public static CodeGenerationContext AddUsing(this CodeGenerationContext context, Type type)
        {
            context.AddUsing(type.Namespace);
            return context;
        }
    }
}