using Newtonsoft.Json;

namespace Dibix.Http.Client
{
    public sealed class HttpResponseContentOptions
    {
        public DateTimeZoneHandling DateTimeZoneHandling { get; set; } = DateTimeZoneHandling.Local;
    }
}