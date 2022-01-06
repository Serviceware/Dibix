using System.Diagnostics;

namespace Dibix
{
    [DebuggerDisplay("{Name,nq} ({GetType().FullName,nq})")]
    public abstract class ActionParameterSourceDefinition
    {
        public abstract string Name { get; }
    }

    public abstract class ActionParameterSourceDefinition<TSource> : ActionParameterSourceDefinition where TSource : ActionParameterSourceDefinition, new()
    {
        public static TSource Instance { get; } = new TSource();
        public static string SourceName => Instance.Name;
    }
}