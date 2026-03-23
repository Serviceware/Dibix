using System;
using System.Threading;

namespace Dibix.Testing
{
    internal static class TestNameAccessor
    {
        private static readonly AsyncLocal<string> CurrentTestName = new AsyncLocal<string>();

        public static string TestName
        {
            get => CurrentTestName.Value ?? throw new InvalidOperationException("Test name not available in the current execution context");
            set => CurrentTestName.Value = value;
        }
    }
}