using System;

namespace Dibix
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DateTimeKindAttribute : Attribute
    {
        public DateTimeKind Kind { get; }

        public DateTimeKindAttribute(DateTimeKind kind)
        {
            this.Kind = kind;
        }
    }
}