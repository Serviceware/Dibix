namespace Dibix.Http.Server
{
    public sealed class HttpParameterConstantSource : HttpParameterSource
    {
        public object Value { get; }
        public override string Description => $"{Value}";

        internal HttpParameterConstantSource(object value)
        {
            this.Value = value;
        }
    }
}