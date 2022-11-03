using System;

namespace Dibix.Http.Server
{
    public sealed class HttpParameterConstantSource : HttpParameterSource
    {
        public Type Type { get; }
        public object Value { get; }
        public override string Description => $"{Value}";

        internal HttpParameterConstantSource(Type type, object value)
        {
            Type = type;
            Value = value;
        }
    }
}