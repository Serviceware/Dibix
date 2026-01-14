using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Dibix.Http.Host.Tests
{
    internal abstract class TestLoggerBase : ILogger
    {
        IDisposable ILogger.BeginScope<TState>(TState state) => EmptyScope.Default;

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        protected abstract void WriteLines(string logLevelString, IEnumerable<string> lines);

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);
            string logLevelString = GetLogLevelString(logLevel);

            string?[] messages = [message, exception?.ToString()];
            string[] lines = messages.Where(x => x != null)
                                     .SelectMany(x => x!.Split('\n'))
                                     .Select(x => x.Trim('\r'))
                                     .ToArray();

            WriteLines(logLevelString, lines);
        }

        private static string GetLogLevelString(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => "none"
        };

        private sealed class EmptyScope : IDisposable
        {
            public static EmptyScope Default { get; } = new EmptyScope();

            void IDisposable.Dispose() { }
        }
    }
}