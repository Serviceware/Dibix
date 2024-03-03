using System;
using System.Diagnostics;
using System.Reflection;

namespace Dibix
{
    [DebuggerDisplay("{Name,nq} ({GetType().FullName,nq})")]
    internal abstract class ActionParameterSourceDefinition
    {
        public abstract string Name { get; }
    }

    internal abstract class ActionParameterSourceDefinition<TSource> : ActionParameterSourceDefinition where TSource : ActionParameterSourceDefinition, new()
    {
        public static string SourceName { get; } = ResolveSourceName();
        public sealed override string Name => SourceName;

        private static string ResolveSourceName()
        {
            Type sourceType = typeof(TSource);
            Type attributeType = typeof(ActionParameterSourceAttribute);
            if (sourceType.GetCustomAttribute(attributeType) is not ActionParameterSourceAttribute attribute)
                throw new InvalidOperationException($"Missing attribute '{attributeType}' on type '{sourceType}'");

            return attribute.SourceName;
        }
    }
}