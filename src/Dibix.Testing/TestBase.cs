using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Testing.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Dibix.Testing
{
    public abstract class TestBase : IDisposable
    {
        #region Fields
        private readonly Assembly _assembly;
        private TestContext _testContext;
        private TestOutputWriter _testOutputHelper;
        private TestResultComposer _testResultComposer;
        private IDisposable _unhandledExceptionDiagnostics;
        #endregion

        #region Properties
        public TestContext TestContext
        {
            get => SafeGetProperty(ref _testContext);
            set => _testContext = value;
        }
        internal TestOutputWriter TestOutputHelper
        {
            get => SafeGetProperty(ref _testOutputHelper);
            private set => _testOutputHelper = value;
        }
        protected virtual bool AttachOutputObserver => false;
        protected virtual TestConfigurationValidationBehavior ConfigurationValidationBehavior => TestDefaults.ValidationBehavior;
        protected virtual TextWriter Out => TestOutputHelper;
        protected string RunDirectory => TestResultComposer.RunDirectory;
        protected string TestDirectory => TestResultComposer.TestDirectory;
        protected virtual bool UseDedicatedTestResultsDirectory => true;
        protected bool IsAssemblyInitialize { get; private set; }
        private TestResultComposer TestResultComposer
        {
            get => SafeGetProperty(ref _testResultComposer);
            set => _testResultComposer = value;
        }
        private static Exception AssemblyInitializeException { get; set; }
        #endregion

        #region Constructor
        protected TestBase()
        {
            _assembly = GetType().Assembly;
        }
        #endregion

        #region Public Methods
        [TestInitialize]
        public async Task OnTestInitialize()
        {
            TestResultComposer = new TestResultComposer(TestContext, UseDedicatedTestResultsDirectory, IsAssemblyInitialize);

            // Unfortunately, when an exception occurs during AssemblyInitialize, the result attachments are no longer collected.
            // Therefore, we delay throwing the exception until the first test is running.
            // We place it just after the TestResultComposer is initialized and prepared the existing run attachments
            if (AssemblyInitializeException != null)
                throw AssemblyInitializeException;

            string outputFileName = IsAssemblyInitialize ? "AssemblyInitialize.log" : "Output.log";
            TestOutputHelper = new TestOutputWriter(TestContext, TestResultComposer, outputToFile: true, fileName: outputFileName, IsAssemblyInitialize, tailOutput: AttachOutputObserver);

#if NETCOREAPP
            if (OperatingSystem.IsWindows())
#endif
            {
                _unhandledExceptionDiagnostics = new UnhandledExceptionDiagnostics(TestResultComposer, Out, LogException, ConfigureEventLogDiagnostics);
            }

            await OnTestInitialized().ConfigureAwait(false);
        }
        #endregion

        #region Protected Methods
        // AssemblyInitialize attribute cannot be placed in base class
        // See: https://github.com/microsoft/testfx/issues/757
        // Therefore we provide an entry method for consumers to call
        // We will then call an instance method for the consumer to implement
        protected static async Task AssemblyInitialize<T>(TestContext testContext) where T : TestBase, new()
        {
            try
            {
                using T instance = new T();
                instance.TestContext = testContext;
                instance.IsAssemblyInitialize = true;
                await instance.OnTestInitialize().ConfigureAwait(false);
                await instance.OnAssemblyInitialize().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                AssemblyInitializeException = exception;
            }
        }

        protected virtual Task OnAssemblyInitialize() => Task.CompletedTask;

        protected virtual Task OnTestInitialized() => Task.CompletedTask;

        protected void WriteLine(string message) => TestOutputHelper.WriteLine(message);

        protected string GetEmbeddedResourceContent(string key) => ResourceUtility.GetEmbeddedResourceContent(_assembly, key);

        protected void AssertEqual(string actual, string extension, string message = null, bool normalizeEndings = false)
        {
            string outputName = TestContext.TestName;
            string expectedKey = $"{outputName}.{extension}";
            string expected = GetEmbeddedResourceContent(expectedKey);
            AssertEqual(expected, actual, outputName, extension, message, normalizeEndings);
        }
        protected void AssertEqual(string expected, string actual, string extension, string message = null, bool normalizeLineEndings = false) => AssertEqual(expected, actual, TestContext.TestName, extension, message, normalizeLineEndings);
        protected void AssertEqual(string expected, string actual, string outputName, string extension, string message = null, bool normalizeLineEndings = false)
        {
            string expectedNormalized = expected;
            string actualNormalized = actual;

            if (normalizeLineEndings)
            {
                expectedNormalized = expected.NormalizeLineEndings();
                actualNormalized = actual.NormalizeLineEndings();
            }

            if (Equals(expectedNormalized, actualNormalized))
                return;

            TestResultComposer.AddFileComparison(expectedNormalized, actualNormalized, outputName, extension);
            throw new AssertTextFailedException(expectedNormalized, actualNormalized, message);
        }

        protected void LogException(Exception exception) => TestResultComposer.AddTestFile("AdditionalErrors.txt", $@"An error occured while collecting the last event log errors
{exception}");

        protected virtual void ConfigureEventLogDiagnostics(EventLogDiagnosticsOptions options) { }

        protected string AddTestFile(string fileName) => TestResultComposer.AddTestFile(fileName);
        protected string AddTestFile(string fileName, string content) => TestResultComposer.AddTestFile(fileName, content);

        protected string ImportTestFile(string filePath) => TestResultComposer.ImportTestFile(filePath);

        protected string AddTestRunFile(string fileName) => TestResultComposer.AddTestRunFile(fileName);
        protected string AddTestRunFile(string fileName, string content) => TestResultComposer.AddTestRunFile(fileName, content);
        
        protected string ImportTestRunFile(string filePath) => TestResultComposer.ImportTestRunFile(filePath);

        protected static void AssertAreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual) => Assert.IsTrue(expected.SequenceEqual(actual), "expected.SequenceEqual(actual)");

        protected static TType AssertIsType<TType>(object instance) where TType : class
        {
            if (instance is TType result) 
                return result;

            throw new AssertFailedException($@"Instance is not of the expected type '{typeof(TType)}'
Actual type: {instance?.GetType()}
Value: {instance}");
        }

        protected static TException AssertThrows<TException>(Action action) where TException : Exception => AssertThrows<TException>(() =>
        {
            action();
            return Task.CompletedTask;
        }).Result;
        protected static async Task<TException> AssertThrows<TException>(Func<Task> action) where TException : Exception
        {
            Type expectedExceptionType = typeof(TException);
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (TException exception)
            {
                return exception;
            }
            catch (Exception exception)
            {
                Type actualExceptionType = exception.GetType();
                throw new AssertFailedException($"Expected exception of type '{expectedExceptionType}' but an exception of '{actualExceptionType}' was thrown instead");
            }
            
            throw new AssertFailedException($"Expected exception of type '{expectedExceptionType}' but none was thrown");
        }

        protected static Task Retry(Func<CancellationToken, Task<bool>> retryMethod, CancellationToken cancellationToken = default) => retryMethod.Retry(cancellationToken);
        protected static Task Retry(Func<CancellationToken, Task<bool>> retryMethod, TimeSpan timeout, CancellationToken cancellationToken = default) => retryMethod.Retry(timeout, cancellationToken);
        protected static Task<TResult> Retry<TResult>(Func<CancellationToken, Task<TResult>> retryMethod, Func<TResult, bool> condition, CancellationToken cancellationToken = default) => retryMethod.Retry(condition, cancellationToken);
        protected static Task<TResult> Retry<TResult>(Func<CancellationToken, Task<TResult>> retryMethod, Func<TResult, bool> condition, TimeSpan timeout, CancellationToken cancellationToken = default) => retryMethod.Retry(condition, timeout, cancellationToken);
        #endregion

        #region Private
        private static T SafeGetProperty<T>(ref T field, [CallerMemberName] string propertyName = null) where T : class
        {
            if (field == null)
                throw new InvalidOperationException($"Property '{propertyName}' not initialized");

            return field;
        }
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _unhandledExceptionDiagnostics?.Dispose();
                _testOutputHelper?.Dispose();
                _testResultComposer?.Complete();
            }
        }
        #endregion
    }
    public abstract class TestBase<TConfiguration> : TestBase where TConfiguration : class, new()
    {
        private TConfiguration _configuration;

        protected TConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                    throw new InvalidOperationException("Configuration not initialized");

                return _configuration;
            }
            private set => _configuration = value;
        }

        protected override Task OnTestInitialized()
        {
            Configuration = TestConfigurationLoader.Load<TConfiguration>(base.TestContext, ConfigurationValidationBehavior, AddConfigurationToOutput);
            return Task.CompletedTask;
        }

        protected virtual void OnConfigurationLoaded(TConfiguration configuration) { }

        private void AddConfigurationToOutput(TConfiguration configuration)
        {
            OnConfigurationLoaded(configuration);

            if (!IsAssemblyInitialize)
                _ = AddTestFile("appsettings.json", JsonConvert.SerializeObject(configuration, Formatting.Indented));
        }
    }
}