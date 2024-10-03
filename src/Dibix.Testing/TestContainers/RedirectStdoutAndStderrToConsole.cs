using System;
using DotNet.Testcontainers.Configurations;

namespace Dibix.Testing.TestContainers
{
    // Console.OpenStandardOutput/OpenStandardError does not get logged correctly to the test result
    // See: https://github.com/microsoft/vstest/issues/799
    // Console.WriteLine does. Therefore, we cannot use the built-in feature Consume.RedirectStdoutAndStderrToConsole()
    public sealed class RedirectStdoutAndStderrToConsole() : RedirectStdoutAndStderrToTextWriter(Console.Out, Console.Error), IOutputConsumer;
}