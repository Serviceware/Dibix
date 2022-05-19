using System;
using System.Linq;

namespace Dibix.Http
{
    public static class RouteBuilder
    {
        public static string BuildRoute(string areaName, string controllerName, string childRoute)
        {
            string url = String.Join("/", EnumerableExtensions.Create(areaName, controllerName, childRoute).Where(x => !String.IsNullOrEmpty(x)));
            return url;
        }
    }
}