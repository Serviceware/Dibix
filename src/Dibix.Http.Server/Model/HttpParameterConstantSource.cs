namespace Dibix.Http.Server
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