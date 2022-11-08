namespace Dibix.Sdk.CodeGeneration.CSharp
{
    internal sealed class CSharpPropertyGetter : CSharpPropertyMethod
    {
        public CSharpPropertyGetter(string body, CSharpModifiers modifiers) : base("get", body, modifiers) { }
    }
}