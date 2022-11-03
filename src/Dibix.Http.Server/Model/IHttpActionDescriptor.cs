using System;
using System.Reflection;

namespace Dibix.Http.Server
{
    public interface IHttpActionDescriptor
    {
        MethodInfo Target { get; }
        HttpApiMethod Method { get; }
        Uri Uri { get; }
        string ChildRoute { get; }
        Type BodyContract { get; }
        Type BodyBinder { get; }

        bool TryGetParameter(string parameterName, out HttpParameterSource value);
    }
}