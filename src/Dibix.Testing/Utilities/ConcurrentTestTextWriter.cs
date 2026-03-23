using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace Dibix.Testing
{
    internal sealed class ConcurrentTestTextWriter : TextWriter
    {
        private static readonly ConcurrentDictionary<string, TextWriter> TestListeners = new ConcurrentDictionary<string, TextWriter>();
        private static ConcurrentTestTextWriter _instance;

        public static ConcurrentTestTextWriter Instance => _instance ??= new ConcurrentTestTextWriter();
        public static int ListenerCount => TestListeners.Count;

        public override Encoding Encoding => Encoding.UTF8;

        public static void Register(string testName, TextWriter inner) => TestListeners.GetOrAdd(testName, inner);

        public static void Unregister(string testName) => TestListeners.TryRemove(testName, out _);

        public override void Write(string value)
        {
            TextWriter writer = GetCurrentWriter();
            writer?.Write(value);
        }

        public override void WriteLine(string value)
        {
            TextWriter writer = GetCurrentWriter();
            writer?.WriteLine(value);
        }

        private static TextWriter GetCurrentWriter() => TestListeners[TestNameAccessor.TestName];
    }
}