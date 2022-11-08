namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SecurityScheme
    {
        public string Name { get; }
        public SecuritySchemeKind Kind { get; }

        public SecurityScheme(string name, SecuritySchemeKind kind)
        {
            this.Name = name;
            this.Kind = kind;
        }
    }
}