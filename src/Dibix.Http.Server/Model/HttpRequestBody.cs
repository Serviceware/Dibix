using System;

namespace Dibix.Http.Server
{
    public sealed class HttpRequestBody
    {
        public Type Contract { get; }
        public Type Binder { get; }
        public long? MaxContentLength { get; }

        public HttpRequestBody(Type contract, Type binder, long? maxContentLength)
        {
            Contract = contract;
            Binder = binder;
            MaxContentLength = maxContentLength;
        }
    }
}