using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    public abstract class TestBase<TConfiguration> : TestBase where TConfiguration : class, new()
    {
        private TConfiguration _configuration;

        protected TConfiguration Configuration
        {
            get
            {
                if (this._configuration == null)
                    throw new InvalidOperationException("Configuration not initialized");

                return this._configuration;
            }
            private set => this._configuration = value;
        }

        protected override Task OnTestInitialized()
        {
            this.Configuration = TestConfigurationLoader.Load<TConfiguration>(this.TestContext);
            return Task.CompletedTask;
        }
    }
    
    public abstract class TestBase : IDisposable
    {
        #region Fields
        private readonly Assembly _assembly;
        private TestContext _testContext;
        private TestOutputWriter _testOutputHelper;
        private TestResultComposer _testResultComposer;
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
        protected virtual TextWriter Out => this.TestOutputHelper;
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
            this.TestResultComposer = new TestResultComposer(this.TestContext);
            this.TestOutputHelper = new TestOutputWriter(this.TestContext, this.TestResultComposer, outputToFile: true, tailOutput: this.AttachOutputObserver);
            AppDomain.CurrentDomain.FirstChanceException += this.OnFirstChanceException;

            await this.OnTestInitialized().ConfigureAwait(false);
        }
        #endregion

        #region Protected Methods
        protected virtual Task OnTestInitialized() => Task.CompletedTask;

        protected void WriteLine(string message) => this.TestOutputHelper.WriteLine(message);

        protected string GetEmbeddedResourceContent(string key) => ResourceUtility.GetEmbeddedResourceContent(this._assembly, key);

        protected void AssertEqual(string expected, string actual, string extension, string message = null, bool normalizeLineEndings = false)
        {
            string expectedNormalized = expected;
            string actualNormalized = actual;

            if (normalizeLineEndings)
            {
                expectedNormalized = expected.NormalizeLineEndings();
                actualNormalized = actual.NormalizeLineEndings();
            }

            if (!Equals(expectedNormalized, actualNormalized))
                this.TestResultComposer.AddFileComparison(expectedNormalized, actualNormalized, extension);

            Assert.AreEqual(expectedNormalized, actualNormalized, message);
        }

        protected void LogException(Exception exception) => this.TestResultComposer.AddFile("AdditionalErrors.txt", $@"An error occured while collecting the last event log errors
{exception}");

        protected void AddResultFile(string fileName, string content) => this.TestResultComposer.AddFile(fileName, content);

        protected static void AssertAreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual) => Assert.IsTrue(expected.SequenceEqual(actual), "expected.SequenceEqual(actual)");

        protected static TType AssertIsType<TType>(object instance) where TType : class
        {
            if (instance is TType result) 
                return result;

            throw new AssertFailedException($@"Instance is not of the expected type '{typeof(TType)}'
Actual type: {instance?.GetType()}
Value: {instance}");
        }

        protected static TException AssertThrows<TException>(Action action) where TException : Exception
        {
            Type expectedExceptionType = typeof(TException);
            try
            {
                action();
                throw new AssertFailedException($"Expected exception of type '{expectedExceptionType}' but none was thrown");
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
        }

        protected static Task Retry(Func<Task<bool>> retryMethod) => Retry(retryMethod, x => x);
        protected static Task Retry(Func<Task<bool>> retryMethod, TimeSpan timeout) => Retry(retryMethod, x => x, timeout);
        protected static Task<TResult> Retry<TResult>(Func<Task<TResult>> retryMethod, Func<TResult, bool> condition) => Retry(retryMethod, condition, TimeSpan.FromMinutes(30));
        protected static Task<TResult> Retry<TResult>(Func<Task<TResult>> retryMethod, Func<TResult, bool> condition, TimeSpan timeout) => retryMethod.Retry(condition, (int)TimeSpan.FromSeconds(1).TotalMilliseconds, (int)timeout.TotalMilliseconds);
        #endregion

        #region Private Methods
        private void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is UnitTestAssertException)
                return;

            AppDomain.CurrentDomain.FirstChanceException -= this.OnFirstChanceException;

            try
            {
#if NET5_0
                if (OperatingSystem.IsWindows())
#endif
                    this.TestResultComposer.AddLastEventLogErrors();
            }
            catch (Exception exception)
            {
                try
                {
                    this.LogException(exception);
                }
                catch (Exception loggingException)
                {
                    this.TestContext.WriteLine($@"An error occured while collecting the last event log errors
{exception}

Additionally, the following error occured while trying to log the previous error to file
{loggingException}");
                    
                    Console.WriteLine("---");
                    Console.WriteLine();
                    Console.WriteLine(exception);
                    //throw;
                }
            }
        }

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
                this.TestOutputHelper?.Dispose();
                TestResultComposer.Complete();
            }
        }
        #endregion
    }
}