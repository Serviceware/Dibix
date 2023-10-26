using Newtonsoft.Json;

namespace Dibix.Http.Client
{
    public sealed class HttpResponseContentOptions
    {
        public DateTimeZoneHandling DateTimeZoneHandling { get; set; } = DateTimeZoneHandling.Local;
        public bool MakeRelativeUrisAbsolute { get; set; } = true;
    }
}