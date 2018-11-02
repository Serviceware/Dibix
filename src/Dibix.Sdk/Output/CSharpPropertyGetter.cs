namespace Dibix.Sdk
{
    internal sealed class CSharpPropertyGetter : CSharpPropertyMethod
    {
        public CSharpPropertyGetter(string body, CSharpModifiers modifiers) : base("get", body, modifiers) { }
    }
}