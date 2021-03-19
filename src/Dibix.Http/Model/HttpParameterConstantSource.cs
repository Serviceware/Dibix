namespace Dibix.Http
{
    public sealed class HttpParameterConstantSource : HttpParameterSource
    {
        public object Value { get; }

        internal HttpParameterConstantSource(object value)
        {
            this.Value = value;
        }
    }
}