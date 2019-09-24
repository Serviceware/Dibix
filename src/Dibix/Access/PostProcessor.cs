using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix
{
    internal static class PostProcessor
    {
        private static ICollection<Func<IPostProcessor>> PostProcessors { get; }

        static PostProcessor()
        {
            PostProcessors = new Collection<Func<IPostProcessor>>();
            Register<RecursiveMapper>();
        }

        public static IEnumerable<TReturn> PostProcess<TReturn>(IEnumerable<TReturn> source, params IPostProcessor[] prePostProcessors)
        {
            IEnumerable<IPostProcessor> postProcessors = (prePostProcessors ?? new IPostProcessor[0]).Concat(PostProcessors.Select(postProcessorFactory => postProcessorFactory()));
            return postProcessors.Aggregate(source, (current, postProcessor) => postProcessor.PostProcess(current));
        }

        private static void Register<T>() where T : IPostProcessor, new()
        {
            PostProcessors.Add(() => new T());
        }
    }
}