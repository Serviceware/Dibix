using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Dibix.Http.Host.Tests
{
    internal sealed class TestOutputLoggerProvider : ILoggerProvider
    {
        private readonly TextWriter _output;

        public TestOutputLoggerProvider(string outputFilePath) => _output = File.CreateText(outputFilePath);

        ILogger ILoggerProvider.CreateLogger(string categoryName) => new TextWriterLogger(categoryName, _output);

        void IDisposable.Dispose() => _output.Dispose();
    }
}