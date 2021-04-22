﻿using System;
using System.IO;
using System.Runtime.ExceptionServices;
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
                TestOutputWriter testOutputHelper = new TestOutputWriter(value, this.LogFileName, this.AttachOutputObserver);
                this.TestOutputHelper = testOutputHelper;

                this.OnTestContextInitialized();
            }
        }
        internal TestOutputWriter TestOutputHelper { get; private set; }
        protected virtual bool AttachOutputObserver => false;
        protected virtual string LogFileName => this.TestContext?.TestName;
        protected virtual TextWriter Out => this.TestOutputHelper;
        #endregion

        #region Constructor
        protected TestBase()
        {
            AppDomain.CurrentDomain.FirstChanceException += this.OnFirstChanceException;
        }
        #endregion

        #region Protected Methods
        protected virtual void OnTestContextInitialized() { }

        protected TConfiguration LoadConfiguration() => TestConfigurationLoader.Load<TConfiguration>(this.TestContext);

        protected void WriteLine(string message) => this.TestOutputHelper.WriteLine(message);

        protected void AssertEqualDiffTool(string expected, string actual, string message = null)
        {
            if (!Equals(expected, actual))
                this.TestContext.AddDiffToolInvoker(expected, actual);

            Assert.AreEqual(expected, actual, message);
        }

        protected void LogException(Exception exception) => this.TestContext.AddResultFile("AdditionalErrors.txt", exception.ToString());

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
                    this.TestContext.AddLastEventLogErrors();
            }
            catch (Exception exception)
            {
                this.LogException(exception);
            }
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
            }
        }
        #endregion
    }
}