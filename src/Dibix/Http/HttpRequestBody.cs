using System;

namespace Dibix.Http
{
    public sealed class HttpRequestBody
    {
        public Type Contract { get; }
        public Type Binder { get; }

        public HttpRequestBody(Type contract, Type binder)
        {
            this.Contract = contract;
            this.Binder = binder;
        }
    }
}