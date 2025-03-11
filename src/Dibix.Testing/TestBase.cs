using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Testing.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Dibix.Testing
{
    public abstract class TestBase : IDisposable
    {
        #region Fields
        private readonly Assembly _assembly;
        private TestContext _testContext;
        private TestOutputWriter _testOutputHelper;
        private TestResultFileManager _testResultFileManager;
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
        protected string RunDirectory => TestResultFileManager.RunDirectory;
        protected string TestDirectory => TestResultFileManager.TestDirectory;
        protected virtual bool UseDedicatedTestResultsDirectory => true;
        protected TestClassInstanceScope Scope { get; private set; }
        private TestResultFileManager TestResultFileManager
        {
            get => SafeGetProperty(ref _testResultFileManager);
            set => _testResultFileManager = value;
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
        public async Task TestInitialize()
        {
            Scope = TestClassInstanceScope.TestInitialize;
            await OnTestInitialize().ConfigureAwait(false);
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
                instance.Scope = TestClassInstanceScope.AssemblyInitialize;
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

        protected void AssertEqual(string actual, string extension, string message = null, bool normalizeEndings = true)
        {
            string outputName = TestContext.TestName;
            string expectedKey = $"{outputName}.{extension}";
            string expected = GetEmbeddedResourceContent(expectedKey);
            AssertEqual(expected, actual, outputName, extension, message, normalizeEndings);
        }
        protected void AssertEqual(string expected, string actual, string extension, string message = null, bool normalizeLineEndings = true) => AssertEqual(expected, actual, TestContext.TestName, extension, message, normalizeLineEndings);
        protected void AssertEqual(string expected, string actual, string outputName, string extension, string message = null, bool normalizeLineEndings = true)
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

            TestResultFileManager.AddFileComparison(expectedNormalized, actualNormalized, outputName, extension);
            throw new AssertTextFailedException(expectedNormalized, actualNormalized, message);
        }

        protected void AssertJsonResponse<T>(T response, Action<JsonSerializerSettings> configureSerializer = null, string outputName = null, string expectedText = null)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
                Formatting = Formatting.Indented,
                DateTimeZoneHandling = DateTimeZoneHandling.Unspecified,
                Converters = { new StringEnumConverter() }
            };
            configureSerializer?.Invoke(settings);

            const string extension = "json";
            string outputNameResolved = outputName ?? TestContext.TestName;
            string expectedTextResolved = expectedText ?? GetEmbeddedResourceContent($"{outputNameResolved}.{extension}");
            string actualText = JsonConvert.SerializeObject(response, settings);
            JToken actualTextDom = JToken.Parse(actualText);

            // Replace JSON path placeholders in the expected text with values from the actual text
            // This is useful for undeterministic values like IDs
            // Example:
            // {
            //   "values": [
            //     {
            //       "id": "{values[0].id}"
            //     }
            //   ]
            // }
            string expectedTextReplaced = Regex.Replace(expectedTextResolved, """
                                                                              "{(?<path>([A-Za-z]+(\[[\d]+\])?)(\.([A-Za-z]+(\[[\d]+\])?)){0,})\}"
                                                                              """, x =>
            {
                string path = x.Groups["path"].Value;
                if (actualTextDom.SelectToken(path) is not JValue jsonValue || jsonValue.Value == null)
                    throw new InvalidOperationException($"Replace pattern did not match a JSON path in the actual document: {path} ({x.Index})");

                StringBuilder sb = new StringBuilder();
                using TextWriter textWriter = new StringWriter(sb);
                using JsonWriter writer = new JsonTextWriter(textWriter);
                jsonValue.WriteTo(writer);
                string replacement = sb.ToString();
                return replacement;
            });

            AssertEqual(expectedTextReplaced, actualText, outputNameResolved, extension: extension);
        }

        protected void LogException(Exception exception) => TestResultFileManager.AddTestFile("AdditionalErrors.txt", $@"An error occured while collecting the last event log errors
{exception}");

        protected virtual void ConfigureEventLogDiagnostics(EventLogDiagnosticsOptions options) { }

        protected string AddTestFile(string fileName) => TestResultFileManager.AddTestFile(fileName);
        protected string AddTestFile(string fileName, string content) => TestResultFileManager.AddTestFile(fileName, content);

        protected string ImportTestFile(string filePath) => TestResultFileManager.ImportTestFile(filePath);

        protected string AddTestRunFile(string fileName) => TestResultFileManager.AddTestRunFile(fileName);
        protected string AddTestRunFile(string fileName, string content) => TestResultFileManager.AddTestRunFile(fileName, content);
        
        protected string ImportTestRunFile(string filePath) => TestResultFileManager.ImportTestRunFile(filePath);

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
        private async Task OnTestInitialize()
        {
            TestResultFileManager = TestResultFileManager.FromTestContext(TestContext, UseDedicatedTestResultsDirectory, Scope);

            // Unfortunately, when an exception occurs during AssemblyInitialize, the result attachments are no longer collected.
            // Therefore, we delay throwing the exception until the first test is running.
            // We place it just after the TestResultFileManager is initialized and prepared the existing run attachments
            if (AssemblyInitializeException != null)
                throw AssemblyInitializeException;

            TestOutputHelper = new TestOutputWriter(TestContext, TestResultFileManager, outputToFile: true, Scope, tailOutput: AttachOutputObserver);
            WriteLine($"Starting execution of test: {TestContextUtility.GetTestName(TestContext)}");

#if NETCOREAPP
            if (OperatingSystem.IsWindows())
#endif
            {
                _unhandledExceptionDiagnostics = new UnhandledExceptionDiagnostics(TestResultFileManager, Out, LogException, ConfigureEventLogDiagnostics);
            }

            await OnTestInitialized().ConfigureAwait(false);
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
            // Not a big fan of this, but subsequent Dispose calls might cause exceptions because the TestBase is an uninitialized state
            if (AssemblyInitializeException != null)
                return;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _unhandledExceptionDiagnostics?.Dispose();
                _testOutputHelper?.Dispose();
                _testResultFileManager?.Complete();
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

            if (Scope == TestClassInstanceScope.TestInitialize)
                _ = AddTestFile("appsettings.json", JsonConvert.SerializeObject(configuration, Formatting.Indented));
        }
    }
}