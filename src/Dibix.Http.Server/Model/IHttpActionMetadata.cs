using System;

namespace Dibix.Http.Server
{
    public interface IHttpActionMetadata
    {
        EndpointMetadata Metadata { get; }
        HttpApiMethod Method { get; }
        Uri Uri { get; }
        Type BodyContract { get; }
    }
}