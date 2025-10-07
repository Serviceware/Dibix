using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    public interface ITestContextFacade
    {
        TestContext TestContext { get; }
        TextWriter Out { get; }
        TestClassInstanceScope Scope { get; }

        string AddTestFile(string fileName, string content);
        string AddTestRunFile(string fileName);
        string ImportTestRunFile(string filePath);
    }

    public interface ITestContextFacade<out TConfiguration> : ITestContextFacade
    {
        TConfiguration Configuration { get; }
    }
}