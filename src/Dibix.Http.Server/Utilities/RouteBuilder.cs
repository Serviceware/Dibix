using System;
using System.Linq;

namespace Dibix.Http.Server
{
    public static class RouteBuilder
    {
        public static string BuildRoute(string areaName, string controllerName, string childRoute)
        {
            string url = String.Join("/", new[] { areaName, controllerName, childRoute }.Where(x => !String.IsNullOrEmpty(x)));
            return url;
        }
    }
}