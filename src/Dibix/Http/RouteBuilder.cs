using System;
using System.Linq;

namespace Dibix.Http
{
    public static class RouteBuilder
    {
        public static string BuildRoute(string areaName, string controllerName, string childRoute)
        {
            return $"api/{String.Join("/", new[] { areaName, controllerName, childRoute }.Where(x => !String.IsNullOrEmpty(x)))}";
        }
    }
}