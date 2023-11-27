using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public interface IHttpRequestDescriptor
    {
        Task<Stream> GetBody();
        IEnumerable<string> GetHeaderValues(string name);
        IEnumerable<string> GetAcceptLanguageValues();
        ClaimsPrincipal GetUser();
        HttpActionDefinition GetActionDefinition();
        string GetRemoteAddress();
        string GetRemoteName();
        string GetBearerToken();
        DateTime? GetBearerTokenExpiresIn();
    }
}