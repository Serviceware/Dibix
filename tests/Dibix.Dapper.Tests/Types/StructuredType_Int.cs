using System;
using System.Collections.Generic;

namespace Dibix.Dapper.Tests
{
    internal class StructuredType_Int : StructuredType_IntStringDecimal
    {
        public void Add(int intValue) => base.Add(intValue, String.Empty, default);

        public static StructuredType_Int From(IEnumerable<int> source, Action<StructuredType_Int, int> addItemFunc)
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsNotNull(addItemFunc, nameof(addItemFunc));

            StructuredType_Int type = new StructuredType_Int();
            foreach (int item in source)
            {
                addItemFunc(type, item);
            }
            return type;
        }
    }
}