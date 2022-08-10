using System;

namespace Dibix.Generators
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class TestMethodGenerationAttribute : Attribute
    {
        public Type BaseType { get; }
        public string? Namespace { get; set; }

        public TestMethodGenerationAttribute(Type baseType, string? @namespace = null)
        {
            this.BaseType = baseType;
        }
    }
}