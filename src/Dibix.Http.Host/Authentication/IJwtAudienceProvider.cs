using System.Collections.Generic;

namespace Dibix.Http.Host
{
    internal interface IJwtAudienceProvider
    {
        ICollection<string> GetValidAudiences();
    }
}