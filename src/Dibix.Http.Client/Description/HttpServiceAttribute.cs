using System;

namespace Dibix.Http.Client
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HttpServiceAttribute : Attribute
    {
        public Type ContractType { get; }

        public HttpServiceAttribute(Type contractType)
        {
            this.ContractType = contractType;
        }
    }
}