using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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

        public static IEnumerable<TReturn> PostProcess<TReturn>(this IEnumerable<TReturn> source, params IPostProcessor[] prePostProcessors) => PostProcess(source, typeof(TReturn), prePostProcessors);
        public static Task<IEnumerable<TReturn>> PostProcess<TReturn>(this Task<IEnumerable<TReturn>> source, params IPostProcessor[] prePostProcessors) => source.ContinueWith(x => PostProcess(x.Result, typeof(TReturn), prePostProcessors));
        public static object PostProcess(object source) => PostProcess(EnumerableExtensions.Create(source), source.GetType(), Enumerable.Empty<IPostProcessor>()).FirstOrDefault();

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