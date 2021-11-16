using System.Net.Http.Formatting;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dibix.Http.Client
{
    internal sealed class HttpClientJsonContractResolver : JsonContractResolver
    {
        private readonly string _hostName;

        public HttpClientJsonContractResolver(string hostName, MediaTypeFormatter formatter) : base(formatter)
        {
            this._hostName = hostName;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (member.IsDefined(typeof(RelativeHttpsUrlAttribute)))
                property.Converter = new RelativeUriConverter(this._hostName);

            return property;
        }
    }
}