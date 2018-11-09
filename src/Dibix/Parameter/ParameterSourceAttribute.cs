using System;

namespace Dibix
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ParameterSourceAttribute : Attribute
    {
        public string Source { get; }

        public ParameterSourceAttribute(string source)
        {
            this.Source = source;
        }
    }
}
