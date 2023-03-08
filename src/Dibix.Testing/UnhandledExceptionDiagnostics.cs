﻿using System;
using System.IO;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing
{
    // Attach last 10 EventLog entries in case of an unhandled exception
#if NETCOREAPP
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    internal sealed class UnhandledExceptionDiagnostics : IDisposable
    {
        private readonly TextWriter _textLogger;
        private readonly Action<Exception> _exceptionLogger;
        private readonly TestResultComposer _testResultComposer;

        public UnhandledExceptionDiagnostics(TextWriter textLogger, Action<Exception> exceptionLogger, TestResultComposer testResultComposer)
        {
            _textLogger = textLogger;
            _exceptionLogger = exceptionLogger;
            _testResultComposer = testResultComposer;
            AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        }

        void IDisposable.Dispose()
        {
            AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
        }

        private void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is UnitTestAssertException)
                return;

            AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;

            try
            {
                _testResultComposer.AddLastEventLogEntries();
            }
            catch (Exception exception)
            {
                try
                {
                    _exceptionLogger(exception);
                }
                catch (Exception loggingException)
                {
                    _textLogger.WriteLine($@"An error occured while collecting the last event log errors
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
    }
}