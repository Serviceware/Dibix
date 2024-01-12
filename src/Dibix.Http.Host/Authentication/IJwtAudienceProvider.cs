using System.Collections.Generic;

namespace Dibix.Http.Host
{
    internal interface IJwtAudienceProvider
    {
        IEnumerable<string>? GetValidAudiences();
    }
}