namespace Dibix.Testing.Generators
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Assembly)]
    internal sealed class TestMethodGenerationAttribute : global::System.Attribute
    {
        public global::System.Type BaseType { get; }
        public string Namespace { get; set; }

#pragma warning disable CS8625
#pragma warning disable CS8618
        public TestMethodGenerationAttribute(global::System.Type baseType, string @namespace = null)
#pragma warning restore CS8618
#pragma warning restore CS8625
        {
            this.BaseType = baseType;
        }
    }
}