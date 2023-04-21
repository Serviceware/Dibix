using System;

namespace Dibix.Testing
{
    public sealed class EventLogDiagnosticsOptions
    {
        public string LogName { get; set; } = "Application";
        public string MachineName { get; set; } = ".";
        public string Source { get; set; } = "";
        public EventLogEntryType Type { get; set; } = EventLogEntryType.Error | EventLogEntryType.Warning;
        public DateTime? Since { get; set; }
        public int Count { get; set; } = 10;

        // Exposing the original enum System.Diagnostics.EventLogEntryType causes the coverlet.collector to hang.
        // Might be related to: https://github.com/coverlet-coverage/coverlet/issues/1044
        [Flags]
        public enum EventLogEntryType
        {
            Error = 1,
            Warning = 2,
            Information = 4,
            SuccessAudit = 8,
            FailureAudit = 16
        }
    }
}