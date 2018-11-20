using System;

namespace Dibix.Sdk
{
    [Flags]
    internal enum ConstraintScope
    {
        None = 0,
        Table = 1,
        Column = 2,
        All = Table | Column
    }
}