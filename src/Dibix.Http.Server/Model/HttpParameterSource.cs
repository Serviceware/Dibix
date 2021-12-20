namespace Dibix.Http.Server
{
    public abstract class HttpParameterSource
    {
        public abstract string Description { get; }

        protected HttpParameterSource() { }
    }
}