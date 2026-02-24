using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Dibix.Tests;

namespace Dibix.Http.Host.Tests
{
    internal sealed class TestClaimsTransformer : IClaimsTransformer
    {
        public Task TransformAsync(ClaimsPrincipal claimsPrincipal)
        {
            string? sqlErrorNumberValue = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "SqlErrorNumber")?.Value;
            if (!Int32.TryParse(sqlErrorNumberValue, out int sqlErrorNumber))
                return Task.CompletedTask;

            string? validationErrorMessage = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "ValidationErrorMessage")?.Value;
            throw DatabaseAccessExceptionFactory.CreateException(sqlErrorNumber, validationErrorMessage ?? "Oops");
        }
    }
}