using System.Collections.Generic;

namespace Dibix
{
    internal interface IPostProcessor
    {
        IEnumerable<TReturn> PostProcess<TReturn>(IEnumerable<TReturn> source);
    }
}