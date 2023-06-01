using System;
using System.Collections.Generic;

namespace Dibix
{
    internal interface IPostProcessor
    {
        IEnumerable<T> PostProcess<T>(IEnumerable<T> source, Type type);
    }
}