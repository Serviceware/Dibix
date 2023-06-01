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
            Register<EntityDescriptorPostProcessor>();
            Register<RecursiveMapper>();
        }

        public static IEnumerable<TReturn> PostProcess<TReturn>(IEnumerable<TReturn> source, params IPostProcessor[] prePostProcessors) => PostProcess(source, typeof(TReturn), prePostProcessors);
        public static IEnumerable<object> PostProcess(IEnumerable<object> source, Type type, params IPostProcessor[] prePostProcessors) => PostProcess<object>(source, type, prePostProcessors);

        private static IEnumerable<T> PostProcess<T>(IEnumerable<T> source, Type type, IEnumerable<IPostProcessor> prePostProcessors)
        {
            IEnumerable<IPostProcessor> postProcessors = prePostProcessors.Concat(PostProcessors.Select(postProcessorFactory => postProcessorFactory()));
            return postProcessors.Aggregate(source, (current, postProcessor) => postProcessor.PostProcess(current, type));
        }

        private static void Register<T>() where T : IPostProcessor, new()
        {
            PostProcessors.Add(() => new T());
        }
    }
}