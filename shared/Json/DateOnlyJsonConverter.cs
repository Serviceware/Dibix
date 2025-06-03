using Newtonsoft.Json.Converters;

namespace Dibix.Http
{
    public sealed class DateOnlyJsonConverter : IsoDateTimeConverter
    {
        public DateOnlyJsonConverter() => DateTimeFormat = "yyyy'-'MM'-'dd";
    }
}