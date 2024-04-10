using System;
using System.Reflection;

namespace Dibix.Http.Server
{
    internal interface IHttpActionDescriptor : IHttpActionMetadata
    {
        MethodInfo Target { get; }
        string ChildRoute { get; }
        Type BodyBinder { get; }

        bool TryGetParameter(string parameterName, out HttpParameterSource value);
        void AppendRequiredClaim(string claimType);
    }
}