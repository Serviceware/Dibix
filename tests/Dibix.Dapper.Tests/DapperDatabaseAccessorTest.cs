using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Dapper.Tests
{
    [TestClass]
    public sealed class DapperDatabaseAccessorTest : DapperTestBase
    {
        [TestMethod]
        public Task QuerySingleAsync_WithMultipleRows_ThrowsException() => ExecuteTest(async accessor =>
        {
            const string commandText = "SELECT 1 UNION ALL SELECT 2";
            DatabaseAccessException exception = await Assert.ThrowsExactlyAsync<DatabaseAccessException>(() => accessor.QuerySingleAsync<int>(commandText, CommandType.Text, ParametersVisitor.Empty, default)).ConfigureAwait(false);
            Assert.AreEqual("""
                            Sequence contains more than one element
                            CommandType: Text
                            CommandText: <Inline>
                            """, exception.Message);
            Assert.AreEqual(DatabaseAccessErrorCode.SequenceContainsMoreThanOneElement, exception.AdditionalErrorCode);
        });

        [TestMethod]
        public Task QuerySingle_WithNoRows_ThrowsException() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT 1 WHERE 1 = 2";
            DatabaseAccessException exception = Assert.ThrowsExactly<DatabaseAccessException>(() => accessor.QuerySingle<int>(commandText, CommandType.Text, ParametersVisitor.Empty));
            Assert.AreEqual("""
                            Sequence contains no elements
                            CommandType: Text
                            CommandText: <Inline>
                            """, exception.Message);
            Assert.AreEqual(DatabaseAccessErrorCode.SequenceContainsNoElements, exception.AdditionalErrorCode);
        });

        [TestMethod]
        public Task QuerySingleOrDefaultAsync_WithMultipleRows_ThrowsException() => ExecuteTest(async accessor =>
        {
            const string commandText = "SELECT 1 UNION ALL SELECT 2";
            DatabaseAccessException exception = await Assert.ThrowsExactlyAsync<DatabaseAccessException>(() => accessor.QuerySingleOrDefaultAsync<int>(commandText, CommandType.Text, ParametersVisitor.Empty, default)).ConfigureAwait(false);
            Assert.AreEqual("""
                            Sequence contains more than one element
                            CommandType: Text
                            CommandText: <Inline>
                            """, exception.Message);
            Assert.AreEqual(DatabaseAccessErrorCode.SequenceContainsMoreThanOneElement, exception.AdditionalErrorCode);
        });

        [TestMethod]
        public Task ReadSingleAsync_WithMultipleRows_ThrowsException() => ExecuteTest(async accessor =>
        {
            const string commandText = "SELECT 1 SELECT 1 UNION ALL SELECT 2";
            using IMultipleResultReader reader = await accessor.QueryMultipleAsync(commandText, CommandType.Text, ParametersVisitor.Empty, default).ConfigureAwait(false);
            _ = await reader.ReadSingleAsync<int>().ConfigureAwait(false);
            DatabaseAccessException exception = await Assert.ThrowsExactlyAsync<DatabaseAccessException>(async () => await reader.ReadSingleAsync<int>().ConfigureAwait(false)).ConfigureAwait(false);
            Assert.AreEqual("""
                            Sequence contains more than one element
                            CommandType: Text
                            CommandText: <Inline>
                            """, exception.Message);
            Assert.AreEqual(DatabaseAccessErrorCode.SequenceContainsMoreThanOneElement, exception.AdditionalErrorCode);
        });

        [TestMethod]
        public Task ReadSingle_WithNoRows_ThrowsException() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT 1 SELECT 1 WHERE 1 = 2";
            using IMultipleResultReader reader = accessor.QueryMultiple(commandText, CommandType.Text, ParametersVisitor.Empty);
            _ = reader.ReadSingle<int>();
            DatabaseAccessException exception = Assert.ThrowsExactly<DatabaseAccessException>(() => reader.ReadSingle<int>());
            Assert.AreEqual("""
                            Sequence contains no elements
                            CommandType: Text
                            CommandText: <Inline>
                            """, exception.Message);
            Assert.AreEqual(DatabaseAccessErrorCode.SequenceContainsNoElements, exception.AdditionalErrorCode);
        });

        [TestMethod]
        public Task ReadSingleOrDefaultAsync_WithMultipleRows_ThrowsException() => ExecuteTest(async accessor =>
        {
            const string commandText = "SELECT 1 SELECT 1 UNION ALL SELECT 2";
            using IMultipleResultReader reader = accessor.QueryMultiple(commandText, CommandType.Text, ParametersVisitor.Empty);
            _ = reader.ReadSingle<int>();
            DatabaseAccessException exception = await Assert.ThrowsExactlyAsync<DatabaseAccessException>(() => reader.ReadSingleOrDefaultAsync<int>()).ConfigureAwait(false);
            Assert.AreEqual("""
                            Sequence contains more than one element
                            CommandType: Text
                            CommandText: <Inline>
                            """, exception.Message);
            Assert.AreEqual(DatabaseAccessErrorCode.SequenceContainsMoreThanOneElement, exception.AdditionalErrorCode);
        });

        [TestMethod]
        public Task QuerySingleOrDefault_WithNoRows_ReturnsDefault() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT 1 WHERE 1 = 2";
            int result = accessor.QuerySingleOrDefault<int>(commandText, CommandType.Text, ParametersVisitor.Empty);
            Assert.AreEqual(default, result);
        });

        [TestMethod]
        public Task QueryFile_WithStreamParameter_IsAcceptedAsBinary() => ExecuteTest(accessor =>
        {
            byte[] data = { 1, 2 };
            const string commandText = "SELECT @data";
            byte[] result = accessor.QuerySingle<byte[]>(commandText, CommandType.Text, accessor.Parameters().SetFromTemplate(new
            {
                data = new MemoryStream(data)
            }).Build());
            AssertAreEqual(data.AsEnumerable(), result.AsEnumerable());
        });

        [TestMethod]
        public Task Execute_WithOutputParameter_OutputParameterValueIsReturned() => ExecuteTest(accessor =>
        {
            const string commandText = "[dbo].[_dibix_tests_sp1]";
            InputClass input = new InputClass();
            ParametersVisitor parameters = accessor.Parameters()
                                                   .SetInt32("out1", out IOutParameter<int> out1)
                                                   .SetString("out3", out IOutParameter<string> out3)
                                                   .SetFromTemplate(input)
                                                   .Build();
            accessor.Execute(commandText, CommandType.StoredProcedure, parameters);
            Assert.AreEqual(5, out1.Result);
            Assert.IsTrue(input.out2.Result);
            Assert.AreEqual("x", out3.Result);
        });

        private sealed class InputClass
        {
            public IOutParameter<bool> out2 { get; set; }
        }

        [TestMethod]
        public Task QueryMany_WithBinaryParameter_UsingTemplate_Success() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT CAST(@binary AS NVARCHAR(MAX))";
            byte[] binary = Encoding.Unicode.GetBytes("Test");
            ParametersVisitor parameters = accessor.Parameters().SetFromTemplate(new { binary }).Build();
            string result = accessor.QuerySingle<string>(commandText, CommandType.Text, parameters);
            Assert.AreEqual("Test", result);
        });

        [TestMethod]
        public Task QueryMany_WithXElementParameter_UsingTemplate_Success() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT [x].[v].value('@value', 'INT') FROM @xml.nodes(N'root/item') AS [x]([v])";
            XElement xml = XElement.Parse("<root><item value=\"1\" /><item value=\"2\" /></root>");
            ParametersVisitor parameters = accessor.Parameters().SetFromTemplate(new { xml }).Build();
            IList<byte> results = accessor.QueryMany<byte>(commandText, CommandType.Text, parameters).ToArray();
            Assert.HasCount(2, results);
            Assert.AreEqual((byte)1, results[0]);
            Assert.AreEqual((byte)2, results[1]);
        });

        [TestMethod]
        public Task QueryMany_WithXElementParameter_UsingTypedMethod_Success() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT [x].[v].value('@value', 'INT') FROM @xml.nodes(N'root/item') AS [x]([v])";
            XElement xml = XElement.Parse("<root><item value=\"1\" /><item value=\"2\" /></root>");
            ParametersVisitor parameters = accessor.Parameters().SetXml(nameof(xml), xml).Build();
            IList<byte> results = accessor.QueryMany<byte>(commandText, CommandType.Text, parameters).ToArray();
            Assert.HasCount(2, results);
            Assert.AreEqual((byte)1, results[0]);
            Assert.AreEqual((byte)2, results[1]);
        });

        [TestMethod]
        public Task QueryMany_WithXElementResult_Success() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT N'<xml/>'";
            XElement element = accessor.QuerySingle<XElement>(commandText, CommandType.Text, ParametersVisitor.Empty);
            Assert.AreEqual("<xml />", element.ToString());
        });

        [TestMethod]
        public Task QuerySingle_MissingColumnName_ThrowsException() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT 1";
            DatabaseAccessException exception = Assert.ThrowsExactly<DatabaseAccessException>(() => accessor.QuerySingle<Entity>(commandText, CommandType.Text, ParametersVisitor.Empty));
            Assert.AreEqual("""
                            Column name was not specified, therefore it cannot be mapped to type 'Dibix.Dapper.Tests.Entity'
                            CommandType: Text
                            CommandText: <Inline>
                            """, exception.Message);
        });

        [TestMethod]
        public Task QuerySingle_InvalidColumnName_ThrowsException() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT 1 AS [idx]";
            DatabaseAccessException exception = Assert.ThrowsExactly<DatabaseAccessException>(() => accessor.QuerySingle<Entity>(commandText, CommandType.Text, ParametersVisitor.Empty));
            Assert.AreEqual("""
                            Column 'idx' does not match a property on type 'Dibix.Dapper.Tests.Entity'
                            CommandType: Text
                            CommandText: <Inline>
                            """, exception.Message);
        });

        [TestMethod]
        public Task QuerySingle_Success() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT 1 AS [id]";
            Entity result = accessor.QuerySingle<Entity>(commandText, CommandType.Text, ParametersVisitor.Empty);
            Assert.AreEqual(1, result.Id);
            Assert.IsNull(result.Name);
        });

        [TestMethod]
        public Task QuerySingle_PrimitiveResult_Success() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT 1";
            bool result = accessor.QuerySingle<bool>(commandText, CommandType.Text, ParametersVisitor.Empty);
            Assert.IsTrue(result);
        });

        [TestMethod]
        [Ignore("https://github.com/DapperLib/Dapper/issues/1901")]
        public Task QuerySingle_PrimitiveResult_Async_Success() => ExecuteTest(async accessor =>
        {
            const string commandText = "SELECT 1";
            bool result = await accessor.QuerySingleAsync<bool>(commandText, CommandType.Text, ParametersVisitor.Empty, default).ConfigureAwait(false);
            Assert.IsTrue(result);
        });

        [TestMethod]
        public Task QuerySingle_WithPrimitiveParameter_UsingLambdaSyntax_Success() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT @agentid AS [id], N'beef' AS [name]";
            ParametersVisitor parameters = accessor.Parameters().SetInt32("agentid", 6).Build();
            Entity result = accessor.QuerySingle<Entity>(commandText, CommandType.Text, parameters);
            Assert.AreEqual(6, result.Id);
            Assert.AreEqual("beef", result.Name);
            Assert.AreEqual(default, result.Price);
        });

        [TestMethod]
        public Task QuerySingle_WithPrimitiveParameter_UsingLambdaAndTemplateSyntax_Success() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT @agentid AS [id], N'beef' AS [name]";
            ParametersVisitor parameters = accessor.Parameters().SetFromTemplate(new { agentid = 6 }).Build();
            Entity result = accessor.QuerySingle<Entity>(commandText, CommandType.Text, parameters);
            Assert.AreEqual(6, result.Id);
            Assert.AreEqual("beef", result.Name);
            Assert.AreEqual(default, result.Price);
        });

        [TestMethod]
        public Task QuerySingle_WithPrimitiveParameter_UsingVariableSyntax_Success() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT @agentid AS [id], N'beef' AS [name]";
            ParametersVisitor @params = accessor.Parameters()
                                                .SetInt32("agentid", 6)
                                                .Build();
            Entity result = accessor.QuerySingle<Entity>(commandText, CommandType.Text, @params);
            Assert.AreEqual(6, result.Id);
            Assert.AreEqual("beef", result.Name);
            Assert.AreEqual(default, result.Price);
        });

        [TestMethod]
        public Task QuerySingle_WithPrimitiveParameter_UsingVariableAndTemplateSyntax_Success() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT @agentid AS [id], N'beef' AS [name], @direction AS [direction]";
            ParametersVisitor @params = accessor.Parameters()
                                                .SetFromTemplate(new
                                                {
                                                    agentid = 6, direction = (Direction?)Direction.Descending
                                                })
                                                .Build();
            Entity result = accessor.QuerySingle<Entity>(commandText, CommandType.Text, @params);
            Assert.AreEqual(6, result.Id);
            Assert.AreEqual("beef", result.Name);
            Assert.AreEqual(default, result.Price);
            Assert.AreEqual(Direction.Descending, result.Direction);
        });

        [TestMethod]
        public Task SetFromTemplate_WithEnumParameter_DbTypeIsResolvedFromEnumBaseType() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT SQL_VARIANT_PROPERTY(@direction, N'BaseType')";
            ParametersVisitor @params = accessor.Parameters()
                                                .SetFromTemplate(new
                                                {
                                                    direction = Direction.Descending
                                                })
                                                .Build();
            string baseType = accessor.QuerySingle<string>(commandText, CommandType.Text, @params);
            Assert.AreEqual("tinyint", baseType);
        });

        [TestMethod]
        public Task QueryMany_WithTableValueParameter_Success() => ExecuteTest(accessor =>
        {
            const string commandText = """
                                       SELECT [intvalue] AS [id], [stringvalue] AS [name]
                                       FROM @translations
                                       """;
            StructuredType_IntString translationsParam = new StructuredType_IntString
            {
                { 7, "de" },
                { 9, "en" }
            };

            ParametersVisitor parameters = accessor.Parameters().SetStructured("translations", translationsParam).Build();
            IList<Entity> result = accessor.QueryMany<Entity>(commandText, CommandType.Text, parameters).ToArray();
            Assert.HasCount(2, result);
            Assert.AreEqual(7, result[0].Id);
            Assert.AreEqual("de", result[0].Name);
            Assert.AreEqual(default, result[0].Price);
            Assert.AreEqual(9, result[1].Id);
            Assert.AreEqual("en", result[1].Name);
            Assert.AreEqual(default, result[1].Price);
        });

        [TestMethod]
        public Task QueryMultiple_UsingTemplate_Success() => ExecuteTest(accessor =>
        {
            const string commandText = """
                                       SELECT @agentid AS [id], N'beef' AS [name], [decimalvalue] AS [price]
                                       FROM @values
                                       SELECT [intvalue]
                                       FROM @ids
                                       """;

            StructuredType_IntStringDecimal valuesParam = new StructuredType_IntStringDecimal { { 5, "cake", 3.975M } };
            StructuredType_Int idsParam = StructuredType_Int.From(new[] { 1, 2 }, (x, y) => x.Add(y));

            ParametersVisitor parameters = accessor.Parameters()
                                                   .SetFromTemplate(new
                                                   {
                                                       agentid = 6,
                                                       values = valuesParam,
                                                       ids = idsParam
                                                   })
                                                   .Build();
            using (IMultipleResultReader reader = accessor.QueryMultiple(commandText, CommandType.Text, parameters))
            {
                Entity entity = reader.ReadSingle<Entity>();
                IEnumerable<int> ids = reader.ReadMany<int>();

                Assert.AreEqual(6, entity.Id);
                Assert.AreEqual("beef", entity.Name);
                Assert.AreEqual(3.98M, entity.Price);
                AssertAreEqual(new[] { 1, 2 }, ids);
            }
        });

        [TestMethod]
        public Task QueryMultiple_UsingTypedMethod_Success() => ExecuteTest(accessor =>
        {
            const string commandText = """
                                       SELECT @agentid AS [id], N'beef' AS [name], [decimalvalue] AS [price]
                                       FROM @values
                                       SELECT [intvalue]
                                       FROM @ids
                                       """;

            StructuredType_IntStringDecimal valuesParam = new StructuredType_IntStringDecimal { { 5, "cake", 3.975M } };
            StructuredType_Int idsParam = StructuredType_Int.From(new[] { 1, 2 }, (x, y) => x.Add(y));

            ParametersVisitor parameters = accessor.Parameters()
                                                   .SetInt32("agentid", 6)
                                                   .SetStructured("values", valuesParam)
                                                   .SetStructured("ids", idsParam)
                                                   .Build();
            using (IMultipleResultReader reader = accessor.QueryMultiple(commandText, CommandType.Text, parameters))
            {
                Entity entity = reader.ReadSingle<Entity>();
                IEnumerable<int> ids = reader.ReadMany<int>();

                Assert.AreEqual(6, entity.Id);
                Assert.AreEqual("beef", entity.Name);
                Assert.AreEqual(3.98M, entity.Price);
                AssertAreEqual(new[] { 1, 2 }, ids);
            }
        });

        [TestMethod]
        public Task CustomSqlMetadata_MaxLength_ValueTooLarge_ThrowsException() => ExecuteTest(accessor =>
        {
            IParameterBuilder parameterBuilder = accessor.Parameters();
            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() => parameterBuilder.SetStructured("values", new StructuredType_String_Custom { "abc" }));
            Assert.AreEqual("""
                            The value at row 0 for column 'stringValue' has a length of 3 which exceeds the maximum length of the data type (1)
                            -
                            Value: abc
                            """, exception.Message);
        });

        [TestMethod]
        public Task CustomSqlMetadata_Scale_DecimalValueIsRounded() => ExecuteTest(accessor =>
        {
            const string commandText = """
                                       SELECT [decimalvalue]
                                       FROM @values
                                       """;

            ParametersVisitor parameters = accessor.Parameters().SetStructured("values", new StructuredType_Decimal_Custom { 3.975M }).Build();
            decimal result = accessor.QuerySingle<decimal>(commandText, CommandType.Text, parameters);
            Assert.AreEqual(4M, result);
        });

        [TestMethod]
        public Task QueryFileAsync() => ExecuteTest(async accessor =>
        {
            FileEntity file = null;
            try
            {
                file = await accessor.QueryFileAsync("SELECT [filename] = N'image.png', [data] = 0x2", CommandType.Text, ParametersVisitor.Empty, CancellationToken.None);
                Assert.AreEqual("image.png", file.FileName);
                Assert.IsNotNull(file.Data);
                Assert.AreEqual("Dibix.ReaderOwningStream", file.Data.GetType().FullName, "Unexpected stream type");
                using MemoryStream stream = new MemoryStream();
                await file.Data.CopyToAsync(stream).ConfigureAwait(false);
                byte[] data = stream.GetBuffer();
                Assert.HasCount(256, data);
                Assert.AreEqual((byte)2, data[0]);
            }
            finally
            {
                if (file != null)
                    await file.Data.DisposeAsync().ConfigureAwait(false);
            }
        });
    }
}