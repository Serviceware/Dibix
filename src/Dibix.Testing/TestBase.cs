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
            get => SafeGetProperty(ref this._testContext);
            set => this._testContext = value;
        }
        internal TestOutputWriter TestOutputHelper
        {
            get => SafeGetProperty(ref this._testOutputHelper);
            private set => this._testOutputHelper = value;
        }
        protected virtual bool AttachOutputObserver => false;
        protected virtual TestConfigurationValidationBehavior ConfigurationValidationBehavior => TestDefaults.ValidationBehavior;
        protected virtual TextWriter Out => this.TestOutputHelper;
        protected string RunDirectory => this.TestResultComposer.RunDirectory;
        protected string TestDirectory => this.TestResultComposer.TestDirectory;
        protected virtual bool UseDedicatedTestResultsDirectory => true;
        private TestResultComposer TestResultComposer
        {
            get => SafeGetProperty(ref this._testResultComposer);
            set => this._testResultComposer = value;
        }
        #endregion

        #region Constructor
        protected TestBase()
        {
            this._assembly = this.GetType().Assembly;
        }
        #endregion

        #region Public Methods
        [TestInitialize]
        public async Task OnTestInitialize()
        {
            this.TestResultComposer = new TestResultComposer(this.TestContext, this.UseDedicatedTestResultsDirectory);
            this.TestOutputHelper = new TestOutputWriter(this.TestContext, this.TestResultComposer, outputToFile: true, tailOutput: this.AttachOutputObserver);

#if NETCOREAPP
            if (OperatingSystem.IsWindows())
#endif
            {
                _unhandledExceptionDiagnostics = new UnhandledExceptionDiagnostics(TestResultComposer, Out, LogException, ConfigureEventLogDiagnostics);
            }

            await this.OnTestInitialized().ConfigureAwait(false);
        }
        #endregion

        #region Protected Methods
        protected virtual Task OnTestInitialized() => Task.CompletedTask;

        protected void WriteLine(string message) => this.TestOutputHelper.WriteLine(message);

        protected string GetEmbeddedResourceContent(string key) => ResourceUtility.GetEmbeddedResourceContent(this._assembly, key);

        protected void AssertEqual(string expected, string actual, string extension, string message = null, bool normalizeLineEndings = false) => this.AssertEqual(expected, actual, this.TestContext.TestName, extension, message, normalizeLineEndings);
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

        protected void LogException(Exception exception) => this.TestResultComposer.AddFile("AdditionalErrors.txt", $@"An error occured while collecting the last event log errors
{exception}");

        protected virtual void ConfigureEventLogDiagnostics(EventLogDiagnosticsOptions options) { }

        protected string AddResultFile(string filePath) => this.TestResultComposer.AddFile(filePath);
        protected string AddResultFile(string fileName, string content) => this.TestResultComposer.AddFile(fileName, content);

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
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._unhandledExceptionDiagnostics?.Dispose();
                this._testOutputHelper?.Dispose();
                this._testResultComposer?.Complete();
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

        private void AddConfigurationToOutput(TConfiguration configuration) => _ = AddResultFile("appsettings.json", JsonConvert.SerializeObject(configuration, Formatting.Indented));
    }
}