using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SecuritySchemes
    {
        private static readonly SecurityScheme AnonymousScheme = new SecurityScheme("Anonymous", SecuritySchemeKind.None);
        private static readonly SecurityScheme BearerScheme = new SecurityScheme("Bearer", SecuritySchemeKind.Bearer);
        private readonly IDictionary<string, SecurityScheme> _map = new Dictionary<string, SecurityScheme>
        {
            { "Anonymous", AnonymousScheme }
          , { "Bearer",    BearerScheme }
        };

        public static SecurityScheme Anonymous => AnonymousScheme;
        public static SecurityScheme Bearer => BearerScheme;
        public IEnumerable<SecurityScheme> Schemes => _map.Values;

        public bool TryFindSecurityScheme(string name, out SecurityScheme scheme) => _map.TryGetValue(name, out scheme);

        public bool RegisterSecurityScheme(SecurityScheme scheme)
        {
            if (_map.ContainsKey(scheme.Name))
                return false;

            _map.Add(scheme.Name, scheme);
            return true;
        }
    }
}