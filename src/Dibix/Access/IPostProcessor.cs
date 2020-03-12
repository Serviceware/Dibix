using System;
using System.Collections.Generic;

namespace Dibix
{
    internal interface IPostProcessor
    {
        IEnumerable<object> PostProcess(IEnumerable<object> source, Type type);
    }
}