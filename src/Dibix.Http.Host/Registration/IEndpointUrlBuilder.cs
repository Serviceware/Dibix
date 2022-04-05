using System;

namespace Dibix.Http.Host
{
    public interface IEndpointUrlBuilder
    {
        Uri BuildUrl(string areaName, string controllerName, string childRoute);
    }
}