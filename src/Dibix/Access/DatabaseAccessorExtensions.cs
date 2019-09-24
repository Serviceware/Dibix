using System;
using System.Collections.Generic;

namespace Dibix
{
    public static partial class DatabaseAccessorExtensions
    {
        internal static IEnumerable<TReturn> PostProcess<TReturn>(this IEnumerable<TReturn> source) => PostProcessor.PostProcess(source);
        internal static IEnumerable<TReturn> PostProcess<TReturn>(this IEnumerable<TReturn> source, MultiMapper multiMapper) => PostProcessor.PostProcess(source, multiMapper);

        private static IParametersVisitor Build(this Action<IParameterBuilder> configureParameters)
        {
            IParameterBuilder builder = new ParameterBuilder();
            configureParameters(builder);
            return builder.Build();
        }

        private sealed class EmptyParameters : IParametersVisitor
        {
            private static readonly EmptyParameters CachedInstance = new EmptyParameters();
            private EmptyParameters() { }

            public static IParametersVisitor Instance => CachedInstance;

            void IParametersVisitor.VisitParameters(ParameterVisitor visitParameter)
            {
                // No parameters so nothing to do here
            }
        }
    }
}