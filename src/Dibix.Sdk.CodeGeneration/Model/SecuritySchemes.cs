using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class SecuritySchemes
    {
        private static readonly SecurityScheme AnonymousScheme = new SecurityScheme("Anonymous", SecuritySchemeKind.None);
        private static readonly SecurityScheme BearerScheme = new SecurityScheme("Bearer", SecuritySchemeKind.Bearer);
        private static readonly IDictionary<string, SecurityScheme> Map = new Dictionary<string, SecurityScheme>
        {
            { "Anonymous", AnonymousScheme }
          , { "Bearer",    BearerScheme }
        };

        public static SecurityScheme Anonymous => AnonymousScheme;
        public static SecurityScheme Bearer => BearerScheme;
        public static IEnumerable<SecurityScheme> Schemes => Map.Values;

        public static bool TryFindSecurityScheme(string name, out SecurityScheme scheme) => Map.TryGetValue(name, out scheme);
    }
}