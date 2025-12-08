using System.Collections.Generic;

namespace Dibix.Http.Host.Tests
{
    internal sealed class DictionaryLogger : TestLoggerBase
    {
        private readonly string _categoryName;
        private readonly IDictionary<string, IList<string>> _target;

        public DictionaryLogger(string categoryName, IDictionary<string, IList<string>> target)
        {
            _categoryName = categoryName;
            _target = target;
        }

        protected override void WriteLines(string logLevelString, IEnumerable<string> lines)
        {
            if (!_target.TryGetValue(_categoryName, out IList<string> messages))
            {
                messages = new List<string>();
                _target[_categoryName] = messages;
            }

            foreach (string line in lines)
            {
                messages.Add($"{logLevelString} {line}");
            }
        }
    }
}