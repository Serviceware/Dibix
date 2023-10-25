using System.Collections.Generic;
using System.Linq;
using Dibix.Http;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SecuritySchemes
    {
        private static readonly SecurityScheme AnonymousScheme = new SecurityScheme(SecuritySchemeNames.Anonymous, SecuritySchemeKind.None);
        private static readonly SecurityScheme BearerScheme = new SecurityScheme(SecuritySchemeNames.Bearer, SecuritySchemeKind.Bearer);
        private readonly IDictionary<string, SecurityScheme> _map = new[] { AnonymousScheme, BearerScheme }.ToDictionary(x => x.Name);

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