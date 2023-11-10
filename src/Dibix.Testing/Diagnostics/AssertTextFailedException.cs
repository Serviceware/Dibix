using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    public sealed class AssertTextFailedException : AssertFailedException
    {
        public string Expected { get; }
        public string Actual { get; }

        internal AssertTextFailedException(string expected, string actual, string message = null) : base($"Expected:<{expected}>. Actual:<{actual}>. {message}")
        {
            Expected = expected;
            Actual = actual;
        }
    }
}