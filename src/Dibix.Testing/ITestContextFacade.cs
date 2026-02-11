using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    public interface ITestContextFacade
    {
        TestContext TestContext { get; }
        TextWriter Out { get; }
        string RunDirectory { get; }
        string TestDirectory { get; }
        TestClassInstanceScope Scope { get; }

        string AddTestFile(string fileName);
        string AddTestFile(string fileName, string content);
        string AddTestRunFile(string fileName);
        string ImportTestRunFile(string filePath);
        Task Retry(Func<CancellationToken, Task<bool>> retryMethod, CancellationToken cancellationToken = default);
        Task Retry(Func<CancellationToken, Task<bool>> retryMethod, TimeSpan timeout, CancellationToken cancellationToken = default);
        Task<TResult> Retry<TResult>(Func<CancellationToken, Task<TResult>> retryMethod, Func<TResult, bool> condition, CancellationToken cancellationToken = default);
        Task<TResult> Retry<TResult>(Func<CancellationToken, Task<TResult>> retryMethod, Func<TResult, bool> condition, TimeSpan timeout, CancellationToken cancellationToken = default);
    }

    public interface ITestContextFacade<out TConfiguration> : ITestContextFacade
    {
        TConfiguration Configuration { get; }
    }
}