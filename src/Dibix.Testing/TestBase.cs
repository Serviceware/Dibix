using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    public abstract class TestBase<TConfiguration> : IDisposable where TConfiguration : LazyConfiguration, new()
    {
        #region Fields
        private TestContext _testContext;
        #endregion

        #region Properties
        public TestContext TestContext
        {
            get => this._testContext;
            set
            {
                this._testContext = value;
                TestOutputHelper testOutputHelper = new TestOutputHelper(value, this.LogFileName, this.AttachOutputObserver);
                this.TestOutputHelper = testOutputHelper;

                this.OnTestContextInitialized();
            }
        }
        internal TestOutputHelper TestOutputHelper { get; private set; }
        protected virtual bool AttachOutputObserver => false;
        protected virtual string LogFileName => this.TestContext?.TestName;
        protected virtual TextWriter Out => this.TestOutputHelper;
        #endregion

        #region Protected Methods
        protected virtual void OnTestContextInitialized() { }

        protected TConfiguration LoadConfiguration() => TestConfigurationLoader.Load<TConfiguration>(this.TestContext);

        protected void WriteLine(string message) => this.TestOutputHelper.WriteLine(message);

        protected static Task Retry(Func<Task<bool>> retryMethod) => Retry(retryMethod, x => x);
        protected static Task Retry(Func<Task<bool>> retryMethod, TimeSpan timeout) => Retry(retryMethod, x => x, timeout);
        protected static Task<TResult> Retry<TResult>(Func<Task<TResult>> retryMethod, Func<TResult, bool> condition) => Retry(retryMethod, condition, TimeSpan.FromMinutes(30));
        protected static Task<TResult> Retry<TResult>(Func<Task<TResult>> retryMethod, Func<TResult, bool> condition, TimeSpan timeout) => retryMethod.Retry(condition, (int)TimeSpan.FromSeconds(1).TotalMilliseconds, (int)timeout.TotalMilliseconds);
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
            }
        }
        #endregion
    }
}