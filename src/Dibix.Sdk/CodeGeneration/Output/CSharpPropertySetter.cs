namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CSharpPropertySetter : CSharpPropertyMethod
    {
        public CSharpPropertySetter(string body, CSharpModifiers modifiers) : base("set", body, modifiers) { }
    }
}