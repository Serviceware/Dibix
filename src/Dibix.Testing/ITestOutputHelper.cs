using System;

namespace Dibix.Testing
{
    public interface ITestOutputHelper : IDisposable
    {
        void WriteLine(string message);
    }
}