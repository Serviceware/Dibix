using System;
using System.Text;

namespace Dibix.Http.Host
{
    internal sealed class AssemblyEndpointConfigurationUrlBuilder : IEndpointUrlBuilder
    {
        public Uri BuildUrl(string areaName, string controllerName, string childRoute)
        {
            StringBuilder sb = new StringBuilder($"/api/{areaName}/{controllerName}");
            if (!String.IsNullOrEmpty(childRoute))
                sb.Append($"/{childRoute}");

            Uri uri = new Uri(sb.ToString(), UriKind.Relative);
            return uri;
        }
    }
}