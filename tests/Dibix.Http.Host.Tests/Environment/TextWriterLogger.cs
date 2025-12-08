using System;
using System.Collections.Generic;
using System.IO;

namespace Dibix.Http.Host.Tests
{
    internal sealed class TextWriterLogger : TestLoggerBase
    {
        private readonly string _categoryName;
        private readonly TextWriter _writer;

        public TextWriterLogger(string categoryName, TextWriter writer)
        {
            _categoryName = categoryName;
            _writer = writer;
        }

        protected override void WriteLines(string logLevelString, IEnumerable<string> lines)
        {
            foreach (string line in lines)
            {
                _writer.WriteLine($"[{DateTime.Now:O}] {logLevelString} [{_categoryName}] {line}");
            }
            _writer.Flush();
        }
    }
}