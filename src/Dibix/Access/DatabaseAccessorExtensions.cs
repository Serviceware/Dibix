using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dibix
{
    public static partial class DatabaseAccessorExtensions
    {
        internal static TReturn PostProcess<TReturn>(this TReturn source)
        {
            if (Equals(source, default(TReturn)))
                return source;

            return PostProcessor.PostProcess(Enumerable.Repeat(source, 1)).FirstOrDefault();
        }
        internal static Task<TReturn> PostProcess<TReturn>(this Task<TReturn> source) => source.ContinueWith(x => PostProcess(x.Result));
        internal static object PostProcess(this object source) => PostProcessor.PostProcess(Enumerable.Repeat(source, 1), source.GetType()).FirstOrDefault();

        internal static IEnumerable<TReturn> PostProcess<TReturn>(this IEnumerable<TReturn> source) => PostProcessor.PostProcess(source);
        internal static IEnumerable<TReturn> PostProcess<TReturn>(this IEnumerable<TReturn> source, MultiMapper multiMapper) => PostProcessor.PostProcess(source, multiMapper);
        internal static Task<IEnumerable<TReturn>> PostProcess<TReturn>(this Task<IEnumerable<TReturn>> source) => source.ContinueWith(x => PostProcess(x.Result));

        private static ParametersVisitor Build(this Action<IParameterBuilder> configureParameters)
        {
            IParameterBuilder builder = new ParameterBuilder();
            configureParameters(builder);
            return builder.Build();
        }
    }
}