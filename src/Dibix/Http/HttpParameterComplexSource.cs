using System;

namespace Dibix.Http
{
    public sealed class HttpParameterComplexSource : HttpParameterSource
    {
        public Type ContractType { get; }

        internal HttpParameterComplexSource(Type contractType)
        {
            this.ContractType = contractType;
        }
    }
}