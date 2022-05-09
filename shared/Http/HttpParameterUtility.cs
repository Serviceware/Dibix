using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dibix.Http
{
    internal static class HttpParameterUtility
    {
        public static IEnumerable<Group> ExtractPathParameters(string route) => Regex.Matches(route, @"\{(?<parameter>[^}]+)\}")
#if !NETCOREAPP
                                                                                     .Cast<Match>()
#endif
                                                                                     .Select(x => x.Groups["parameter"]);
    }
}