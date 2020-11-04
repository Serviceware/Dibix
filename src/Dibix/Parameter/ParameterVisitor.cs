using System;
using System.Data;

namespace Dibix
{
    public delegate void InputParameterVisitor(string name, object value, Type clrType, DbType? suggestedDataType, bool isOutput);
    public delegate object OutputParameterVisitor(string name);
}