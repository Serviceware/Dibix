using System;

namespace Dibix.Generators
{
    [AttributeUsage(AttributeTargets.Assembly)]
    internal sealed class TestMethodGenerationAttribute : Attribute
    {
        public Type BaseType { get; }
        public string Namespace { get; set; }

#pragma warning disable CS8625
#pragma warning disable CS8618
        public TestMethodGenerationAttribute(Type baseType, string @namespace = null)
#pragma warning restore CS8618
#pragma warning restore CS8625
        {
            this.BaseType = baseType;
        }
    }
}