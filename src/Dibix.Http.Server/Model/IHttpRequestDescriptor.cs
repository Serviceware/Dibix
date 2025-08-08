using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;

namespace Dibix.Http.Server
{
    public interface IHttpRequestDescriptor
    {
        string GetPath();
        Stream GetBody();
        string GetBodyMediaType();
        string GetBodyFileName();
        IEnumerable<string> GetHeaderValues(string name);
        IEnumerable<string> GetAcceptLanguageValues();
        ClaimsPrincipal GetUser();
        HttpActionDefinition GetActionDefinition();
        string GetRemoteAddress();
        string GetRemoteName();
        string GetBearerToken();
        DateTime? GetBearerTokenExpiresAt();
    }
}