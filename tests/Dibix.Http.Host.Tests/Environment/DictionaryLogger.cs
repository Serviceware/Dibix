using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Dibix.Http.Host.Tests
{
    internal sealed class DictionaryLogger : TestLoggerBase
    {
        private readonly string _categoryName;
        private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _target;

        public DictionaryLogger(string categoryName, ConcurrentDictionary<string, ConcurrentBag<string>> target)
        {
            _categoryName = categoryName;
            _target = target;
        }

        protected override void WriteLines(string logLevelString, IEnumerable<string> lines)
        {
            ConcurrentBag<string> messages = _target.GetOrAdd(_categoryName, static _ => new ConcurrentBag<string>());

            foreach (string line in lines)
            {
                messages.Add($"{logLevelString} {line}");
            }
        }
    }
}