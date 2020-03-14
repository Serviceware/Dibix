using System;

namespace Dibix
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class StructuredTypeAttribute : Attribute
    {
        public string UdtName { get; }

        public StructuredTypeAttribute(string udtName)
        {
            this.UdtName = udtName;
        }
    }
}