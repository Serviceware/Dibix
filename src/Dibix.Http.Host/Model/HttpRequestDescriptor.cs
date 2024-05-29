using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Dibix.Http.Host.Extensions;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Host
{
    internal sealed class HttpRequestDescriptor : IHttpRequestDescriptor
    {
        private readonly HttpRequest _request;

        public HttpRequestDescriptor(HttpRequest request)
        {
            _request = request;
        }

        public string GetPath() => $"{_request.PathBase}{_request.Path}";

        public Task<Stream> GetBody() => Task.FromResult(_request.Body);

        public IEnumerable<string> GetHeaderValues(string name) => _request.Headers[name];
        
        public IEnumerable<string> GetAcceptLanguageValues() => _request.GetTypedHeaders().AcceptLanguage.Select(x => x.Value.Value).Where(x => x != null).Select(x => x!);

        public ClaimsPrincipal GetUser() => _request.HttpContext.User;

        public HttpActionDefinition GetActionDefinition() => _request.HttpContext.GetEndpointDefinition().ActionDefinition;

        public string? GetRemoteAddress() => _request.HttpContext.Connection.RemoteIpAddress?.ToString();

        public string? GetRemoteName()
        {
            string? remoteAddress = GetRemoteAddress();
            if (remoteAddress == null) 
                return null;

            try
            {
                return Dns.GetHostEntry(remoteAddress).HostName;
            }
            catch (Exception)
            {
                return remoteAddress;
            }
        }

        public string? GetBearerToken()
        {
            string? authorization = _request.GetTypedHeaders().Headers.Authorization;
            if (String.IsNullOrEmpty(authorization))
                return null;

            string? token = null;
            const string bearerPrefix = "Bearer ";
            if (authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)) 
                token = authorization[bearerPrefix.Length..].Trim();

            return token;
        }

        public DateTime? GetBearerTokenExpiresAt()
        {
            IAuthenticateResultFeature? authenticateResultFeature = _request.HttpContext.Features.Get<IAuthenticateResultFeature>();
            return authenticateResultFeature?.AuthenticateResult?.Properties?.ExpiresUtc?.UtcDateTime;
        }
    }
}