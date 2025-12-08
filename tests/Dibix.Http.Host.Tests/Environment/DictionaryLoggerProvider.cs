using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Dibix.Http.Host.Tests
{
    internal sealed class DictionaryLoggerProvider : ILoggerProvider
    {
        private readonly IDictionary<string, IList<string>> _target;

        public DictionaryLoggerProvider(IDictionary<string, IList<string>> target) => _target = target;

        ILogger ILoggerProvider.CreateLogger(string categoryName) => new DictionaryLogger(categoryName, _target);

        void IDisposable.Dispose() { }
    }
}