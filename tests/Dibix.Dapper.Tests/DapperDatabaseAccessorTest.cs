using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Dibix.Dapper.Tests
{
    public class DapperDatabaseAccessorTest
    {
        [Fact]
        public void QuerySingle_WithMultipleRows_ThrowsException()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = "SELECT 1 UNION ALL SELECT 2";
                DatabaseAccessException exception = Assert.Throws<DatabaseAccessException>(() => accessor.QuerySingle<byte>(commandText));
                Assert.Equal(@"Sequence contains more than one element
CommandType: Text
CommandText: <Dynamic>
", exception.Message);
            }
        }

        [Fact]
        public void Execute_WithOutputParameter_OutputParameterValueIsReturned()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = "[dbo].[_dibix_tests_sp1]";
                IParametersVisitor parameters = accessor.Parameters()
                                                        .SetInt32("out", out IOutParameter<int?> @out)
                                                        .Build();
                accessor.Execute(commandText, CommandType.StoredProcedure, parameters);
                Assert.Equal(5, @out.Result);
            }
        }

        [Fact]
        public void QueryMany_WithXElementParameter_Success()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = "SELECT [x].[v].value('@value', 'INT') FROM @xml.nodes(N'root/item') AS [x]([v])";
                XElement xml = XElement.Parse("<root><item value=\"1\" /><item value=\"2\" /></root>");
                IParametersVisitor parameters = accessor.Parameters().SetFromTemplate(new { xml }).Build();
                IList<byte> results = accessor.QueryMany<byte>(commandText, parameters).ToArray();
                Assert.Equal(2, results.Count);
                Assert.Equal((byte)1, results[0]);
                Assert.Equal((byte)2, results[1]);
            }
        }

        [Fact]
        public void QuerySingle_MissingColumnName_ThrowsException()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT 1";
                DatabaseAccessException exception = Assert.Throws<DatabaseAccessException>(() => accessor.QuerySingle<Entity>(commandText));
                Assert.Equal(@"Column name was not specified, therefore it cannot be mapped to type 'Dibix.Dapper.Tests.Entity'
CommandType: Text
CommandText: <Dynamic>
", exception.Message);
            }
        }

        [Fact]
        public void QuerySingle_InvalidColumnName_ThrowsException()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT 1 AS [idx]";
                DatabaseAccessException exception = Assert.Throws<DatabaseAccessException>(() => accessor.QuerySingle<Entity>(commandText));
                Assert.Equal(@"Column 'idx' does not match a property on type 'Dibix.Dapper.Tests.Entity'
CommandType: Text
CommandText: <Dynamic>
", exception.Message);
            }
        }

        [Fact]
        public void QuerySingle_Success()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT 1 AS [id]";
                Entity result = accessor.QuerySingle<Entity>(commandText);
                Assert.Equal(1, result.Id);
                Assert.Null(result.Name);
            }
        }

        [Fact]
        public void QuerySingle_WithPrimitiveParameter_UsingLambdaSyntax_Success()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT @agentid AS [id], N'beef' AS [name]";
                IParametersVisitor parameters = accessor.Parameters().SetInt32("agentid", 6).Build();
                Entity result = accessor.QuerySingle<Entity>(commandText, parameters);
                Assert.Equal(6, result.Id);
                Assert.Equal("beef", result.Name);
                Assert.Equal(default, result.Price);
            }
        }

        [Fact]
        public void QuerySingle_WithPrimitiveParameter_UsingLambdaAndTemplateSyntax_Success()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT @agentid AS [id], N'beef' AS [name]";
                IParametersVisitor parameters = accessor.Parameters().SetFromTemplate(new { agentid = 6 }).Build();
                Entity result = accessor.QuerySingle<Entity>(commandText, parameters);
                Assert.Equal(6, result.Id);
                Assert.Equal("beef", result.Name);
                Assert.Equal(default, result.Price);
            }
        }

        [Fact]
        public void QuerySingle_WithPrimitiveParameter_UsingVariableSyntax_Success()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT @agentid AS [id], N'beef' AS [name]";
                IParametersVisitor @params = accessor.Parameters()
                                                     .SetInt32("agentid", 6)
                                                     .Build();
                Entity result = accessor.QuerySingle<Entity>(commandText, @params);
                Assert.Equal(6, result.Id);
                Assert.Equal("beef", result.Name);
                Assert.Equal(default, result.Price);
            }
        }

        [Fact]
        public void QuerySingle_WithPrimitiveParameter_UsingVariableAndTemplateSyntax_Success()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT @agentid AS [id], N'beef' AS [name]";
                IParametersVisitor @params = accessor.Parameters()
                                                     .SetFromTemplate(new { agentid = 6 })
                                                     .Build();
                Entity result = accessor.QuerySingle<Entity>(commandText, @params);
                Assert.Equal(6, result.Id);
                Assert.Equal("beef", result.Name);
                Assert.Equal(default, result.Price);
            }
        }

        [Fact]
        public void QueryMany_WithTableValueParameter_Success()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT [intvalue] AS [id], [stringvalue] AS [name]
FROM @translations";
                StructuredType_IntString translationsParam = new StructuredType_IntString
                {
                    { 7, "de" },
                    { 9, "en" }
                };

                IParametersVisitor parameters = accessor.Parameters().SetStructured("translations", translationsParam).Build();
                IList<Entity> result = accessor.QueryMany<Entity>(commandText, parameters).ToArray();
                Assert.Equal(2, result.Count);
                Assert.Equal(7, result[0].Id);
                Assert.Equal("de", result[0].Name);
                Assert.Equal(default, result[0].Price);
                Assert.Equal(9, result[1].Id);
                Assert.Equal("en", result[1].Name);
                Assert.Equal(default, result[1].Price);
            }
        }

        [Fact]
        public void QueryMultiple_Success()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT @agentid AS [id], N'beef' AS [name], [decimalvalue] AS [price]
FROM @values
SELECT [intvalue]
FROM @ids";

                StructuredType_IntStringDecimal valuesParam = new StructuredType_IntStringDecimal { { 5, "cake", 3.975M } };
                StructuredType_Int idsParam = StructuredType_Int.From(new[] { 1, 2 }, (x, y) => x.Add(y));

                IParametersVisitor parameters = accessor.Parameters()
                                                        .SetInt32("agentid", 6)
                                                        .SetStructured("values", valuesParam)
                                                        .SetStructured("ids", idsParam)
                                                        .Build();
                using (IMultipleResultReader reader = accessor.QueryMultiple(commandText, parameters))
                {
                    Entity entity = reader.ReadSingle<Entity>();
                    IEnumerable<int> ids = reader.ReadMany<int>();

                    Assert.Equal(6, entity.Id);
                    Assert.Equal("beef", entity.Name);
                    Assert.Equal(3.98M, entity.Price);
                    Assert.Equal(new[] { 1, 2 }, ids);
                }
            }
        }

        [Fact]
        public void QuerySingle_WithMultiMap_Success()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT [intvalue] AS [id], 0 AS [id], [stringvalue] AS [name], 0 AS [id], [decimalvalue] AS [price]
FROM @values";

                StructuredType_IntStringDecimal valuesParam = new StructuredType_IntStringDecimal { { 5, "cake", 3.975M } };
                IParametersVisitor @params = accessor.Parameters()
                                                     .SetStructured("values", valuesParam)
                                                     .Build();
                void MapEntity(Entity a, NameEntity b, PriceEntity c)
                {
                    Assert.Equal(5, a.Id);
                    Assert.Equal(default, a.Name);
                    Assert.Equal(default, a.Price);

                    Assert.Equal("cake", b.Name);

                    Assert.Equal(3.98M, c.Price);

                    a.Name = b.Name;
                    a.Price = c.Price;
                }

                Entity result = accessor.QuerySingle<Entity, NameEntity, PriceEntity>(commandText, @params, MapEntity, "id,id");
                Assert.Equal(5, result.Id);
                Assert.Equal("cake", result.Name);
                Assert.Equal(3.98M, result.Price);
            }
        }

        [Fact]
        public void QueryMultiple_WithMultiMap_Success()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT [intvalue] AS [id], 0 AS [id], [stringvalue] AS [name], 0 AS [id], [decimalvalue] AS [price]
FROM @values";

                StructuredType_IntStringDecimal valuesParam = new StructuredType_IntStringDecimal { { 5, "cake", 3.975M } };
                IParametersVisitor @params = accessor.Parameters()
                                                     .SetStructured("values", valuesParam)
                                                     .Build();
                void MapEntity(Entity a, NameEntity b, PriceEntity c)
                {
                    Assert.Equal(5, a.Id);
                    Assert.Equal(default, a.Name);
                    Assert.Equal(default, a.Price);

                    Assert.Equal("cake", b.Name);

                    Assert.Equal(3.98M, c.Price);

                    a.Name = b.Name;
                    a.Price = c.Price;
                }

                using (IMultipleResultReader reader = accessor.QueryMultiple(commandText, @params))
                {
                    Entity result = reader.ReadSingle<Entity, NameEntity, PriceEntity>(MapEntity, "id,id");
                    Assert.Equal(5, result.Id);
                    Assert.Equal("cake", result.Name);
                    Assert.Equal(3.98M, result.Price);
                }
            }
        }

        [Fact]
        public void CustomSqlMetadata_MaxLength_TextIsTrimmed()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT [stringvalue]
FROM @values";

                IParametersVisitor parameters = accessor.Parameters().SetStructured("values", new StructuredType_String_Custom { "abc" }).Build();
                string result = accessor.QuerySingle<string>(commandText, parameters);
                Assert.Equal("a", result);
            }
        }

        [Fact]
        public void CustomSqlMetadata_Scale_DecimalValueIsRounded()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT [decimalvalue]
FROM @values";

                IParametersVisitor parameters = accessor.Parameters().SetStructured("values", new StructuredType_Decimal_Custom { 3.975M }).Build();
                decimal result = accessor.QuerySingle<decimal>(commandText, parameters);
                Assert.Equal(4M, result);
            }
        }
    }
}
