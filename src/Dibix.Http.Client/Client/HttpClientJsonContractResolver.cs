using System.Net.Http.Formatting;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dibix.Http.Client
{
    internal sealed class HttpClientJsonContractResolver : JsonContractResolver
    {
        private readonly string _hostName;
        private readonly bool _responseContentMakeRelativeUrisAbsolute;

        public HttpClientJsonContractResolver(string hostName, MediaTypeFormatter formatter, bool responseContentMakeRelativeUrisAbsolute) : base(formatter)
        {
            _hostName = hostName;
            _responseContentMakeRelativeUrisAbsolute = responseContentMakeRelativeUrisAbsolute;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (_responseContentMakeRelativeUrisAbsolute && member.IsDefined(typeof(RelativeHttpsUrlAttribute)))
                property.Converter = new RelativeUriConverter(_hostName);

            return property;
        }
    }
}