using System;
using System.Data;

namespace Dibix
{
    public delegate void ParameterVisitor(string name, object value, Type clrType, DbType? suggestedDataType);
}