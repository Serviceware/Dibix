using System;
using System.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Dapper.Tests
{
    [TestClass]
    public sealed class DatabaseAccessExceptionTest : DapperTestBase
    {
        [TestMethod]
        [Obsolete("BinaryFormatter")]
        public Task Execute_WithError_ThrowsException_ExceptionIsSerializable() => base.ExecuteTest(async accessor =>
        {
            const string commandText = "THROW 50000, N'Oops', 1";
            DatabaseAccessException exception = await AssertThrows<DatabaseAccessException>(() => accessor.ExecuteAsync(commandText, CommandType.Text, ParametersVisitor.Empty, default)).ConfigureAwait(false);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using Stream stream = new MemoryStream();
            binaryFormatter.Serialize(stream, exception);
        });
    }
}