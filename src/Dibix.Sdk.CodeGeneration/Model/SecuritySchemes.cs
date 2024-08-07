﻿using System.Collections.Generic;
using System.Linq;
using Dibix.Http;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SecuritySchemes
    {
        private static readonly SecurityScheme AnonymousScheme = new SecurityScheme(SecuritySchemeNames.Anonymous, value: null);
        private static readonly SecurityScheme BearerScheme = new SecurityScheme(SecuritySchemeNames.Bearer, new BearerSecuritySchemeValue());
        private readonly IDictionary<string, SecurityScheme> _map = new[] { AnonymousScheme, BearerScheme }.ToDictionary(x => x.SchemeName);

        public static SecurityScheme Anonymous => AnonymousScheme;
        public static SecurityScheme Bearer => BearerScheme;
        public IEnumerable<SecurityScheme> Schemes => _map.Values;

        public bool TryFindSecurityScheme(string name, out SecurityScheme scheme) => _map.TryGetValue(name, out scheme);

        public bool RegisterSecurityScheme(SecurityScheme scheme)
        {
            if (_map.ContainsKey(scheme.SchemeName))
                return false;

            _map.Add(scheme.SchemeName, scheme);
            return true;
        }
    }
}