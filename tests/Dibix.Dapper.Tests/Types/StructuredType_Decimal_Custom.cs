using System;

namespace Dibix.Dapper.Tests
{
    internal class StructuredType_Decimal_Custom : StructuredType_IntStringDecimal_Custom
    {
        public void Add(decimal decimalValue) => base.Add(default, String.Empty, decimalValue);
    }
}