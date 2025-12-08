using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Dibix.Http.Host.Tests
{
    internal sealed class TextWriterLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly TextWriter _writer;

        public TextWriterLogger(string categoryName, TextWriter writer)
        {
            _categoryName = categoryName;
            _writer = writer;
        }

        public IDisposable BeginScope<TState>(TState state) => EmptyScope.Default;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string message = formatter(state, exception);
            string logLevelString = GetLogLevelString(logLevel);

            string[] messages = [message, exception?.ToString()];
            string[] lines = messages.Where(x => x != null)
                                     .SelectMany(x => x.Split('\n'))
                                     .Select(x => x.Trim('\r'))
                                     .ToArray();

            foreach (string line in lines)
            {
                _writer.WriteLine($"[{DateTime.Now:O}] {logLevelString} [{_categoryName}] {line}");
            }

            _writer.Flush();
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