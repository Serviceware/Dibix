using System.Data;

namespace Dibix
{
    public delegate void InputParameterVisitor(string name, DbType type, object value, bool isOutput);
    public delegate object OutputParameterVisitor(string name);
}