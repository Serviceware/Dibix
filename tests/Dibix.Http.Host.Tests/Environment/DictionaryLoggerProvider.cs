using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Dibix.Http.Host.Tests
{
    internal sealed class DictionaryLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _target;

        public DictionaryLoggerProvider(ConcurrentDictionary<string, ConcurrentBag<string>> target) => _target = target;

        ILogger ILoggerProvider.CreateLogger(string categoryName) => new DictionaryLogger(categoryName, _target);

        void IDisposable.Dispose() { }
    }
}