using System.Diagnostics;

namespace Dibix
{
    public sealed class DibixTraceSource
    {
        private readonly TraceSource _traceSource;

        public static DibixTraceSource Sql { get; } = new DibixTraceSource("Dibix.Sql");
        public static DibixTraceSource Accessor { get; } = new DibixTraceSource("Dibix.Accessor");

        private DibixTraceSource(string name) => _traceSource = new TraceSource(name);

        public void AddListener(TraceListener listener, SourceLevels level)
        {
            _traceSource.Switch.Level = level;
            _traceSource.Listeners.Add(listener);
        }

        public void RemoveListener(TraceListener listener) => _traceSource.Listeners.Remove(listener);

        internal void TraceInformation(string message) => _traceSource.TraceInformation(message);
    }
}