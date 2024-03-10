using System;

namespace Dibix
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IgnoreSerializationIfEmptyAttribute : Attribute
    {
    }
}