namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CSharpPropertyGetter : CSharpPropertyMethod
    {
        public CSharpPropertyGetter(string body, CSharpModifiers modifiers) : base("get", body, modifiers) { }
    }
}