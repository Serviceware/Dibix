using System;

namespace Dibix.Http.Server
{
    public interface IHttpActionMetadata
    {
        EndpointMetadata Metadata { get; }
        string ActionName { get; }
        string RelativeNamespace { get; }
        HttpApiMethod Method { get; }
        Uri Uri { get; }
        Type BodyContract { get; }
    }
}