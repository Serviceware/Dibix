using System.Data;

namespace Dibix
{
    public delegate void InputParameterVisitor(string name, DbType type, object value, bool isOutput, CustomInputType customInputType);
    public delegate object OutputParameterVisitor(string name);
}