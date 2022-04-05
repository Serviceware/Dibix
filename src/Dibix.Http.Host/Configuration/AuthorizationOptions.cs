﻿namespace Dibix.Http.Host
{
    public sealed class AuthorizationOptions
    {
        public const string ConfigurationSectionName = "Authentication";
        public const string SchemeName = "Dibix";

        public string? Authority { get; set; }
        public string Audience { get; set; } = "dibix";
    }
}