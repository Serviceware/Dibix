using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Moq;
using Xunit;

namespace Dibix.Sdk.Tests
{
    public sealed class SqlMarkupReaderTest
    {
        [Fact]
        public void CrossCheck()
        {
            const string sql = @"-- @Namespace  X
-- @Return ClrTypes:x
 --  @Return Mode:Single y
/* @me: HOW TO TEST IT
   me HOW TO TEST IT?!
@OmitResult 
@Cake Prop1:true Prop2:False;But;True
 */ 
CREATE PROCEDURE [dbo].[sp] /*  @ClrType Dibix.Sdk.VisualStudio.Tests.Direction  */   @param1 INT
AS
    ;";
            TSqlFragment fragment = ScriptDomFacade.Parse(sql);
            ISqlMarkupDeclaration map = SqlMarkupReader.ReadHeader(fragment, null, null);

            Assert.True(map.TryGetSingleElement("Namespace", null, null, out ISqlElement element));
            Assert.Equal(1, element.Line);
            Assert.Equal(4, element.Column);

            Assert.True(map.TryGetSingleElementValue("Namespace", null, null, out string value));
            Assert.Equal("X", value);

            Assert.True(map.HasSingleElement("OmitResult", null, null));

            Assert.True(map.TryGetSingleElement("Cake", null, null, out element));
            Assert.Equal(7, element.Line);
            ISqlElementValue prop1 = element.GetPropertyValue("Prop1");
            Assert.Equal("true", prop1.Value);
            Assert.Equal(7, prop1.Line);
            Assert.Equal(13, prop1.Column);
            ISqlElementValue prop2 = element.GetPropertyValue("Prop2");
            Assert.Equal("False;But;True", prop2.Value);
            Assert.Equal(7, prop2.Line);
            Assert.Equal(24, prop2.Column);

            IList<ISqlElement> returnElements = map.GetElements("Return").ToArray();
            Assert.Equal(2, returnElements.Count);
            Assert.True(returnElements[0].TryGetPropertyValue("ClrTypes", isDefault: true, out ISqlElementValue elementValue));
            Assert.Equal("x", elementValue.Value);
            Assert.Equal(2, elementValue.Line);
            Assert.Equal(21, elementValue.Column);
            Assert.True(returnElements[1].TryGetPropertyValue("ClrTypes", isDefault: true, out elementValue));
            Assert.Equal("y", elementValue.Value);
            Assert.Equal(3, elementValue.Line);
            Assert.Equal(26, elementValue.Column);
            Assert.True(returnElements[1].TryGetPropertyValue("Mode", isDefault: false, out elementValue));
            Assert.Equal(3, elementValue.Line);
            Assert.Equal(19, elementValue.Column);

            ProcedureParameter parameter = ((ProcedureStatementBodyBase)((TSqlScript)fragment).Batches[0].Statements[0]).Parameters[0];
            map = SqlMarkupReader.ReadFragment(parameter, null, null);
            Assert.True(map.TryGetSingleElementValue("ClrType", null, null, out elementValue));
            Assert.Equal("Dibix.Sdk.VisualStudio.Tests.Direction", elementValue.Value);
            Assert.Equal(9, elementValue.Line);
            Assert.Equal(42, elementValue.Column);
        }

        [Fact]
        public void MissingPropertyValue_LogsError()
        {
            const string sql = @"-- @Return Name:
CREATE PROCEDURE [dbo].[sp]
AS
    ;";
            
            Mock<ILogger> logger = new Mock<ILogger>(MockBehavior.Strict);

            logger.Setup(x => x.LogError(null, "Missing value for 'Name' property", null, 1, 12)).Verifiable();

            TSqlFragment fragment = ScriptDomFacade.Parse(sql);
            _ = SqlMarkupReader.ReadHeader(fragment, null, logger.Object);

            logger.Verify();
        }

        [Fact]
        public void DuplicateProperty_LogsError()
        {
            const string sql = @"-- @Return ClrTypes:A ClrTypes:B
CREATE PROCEDURE [dbo].[sp]
AS
    ;";
            
            Mock<ILogger> logger = new Mock<ILogger>(MockBehavior.Strict);

            logger.Setup(x => x.LogError(null, "Duplicate property for @Return.ClrTypes", null, 1, 23)).Verifiable();

            TSqlFragment fragment = ScriptDomFacade.Parse(sql);
            _ = SqlMarkupReader.ReadHeader(fragment, null, logger.Object);

            logger.Verify();
        }

        [Fact]
        public void DuplicateDefaultValue_LogsError()
        {
            const string sql = @"-- @Return A B
CREATE PROCEDURE [dbo].[sp]
AS
    ;";

            Mock<ILogger> logger = new Mock<ILogger>(MockBehavior.Strict);

            logger.Setup(x => x.LogError(null, "Multiple default properties specified for @Return", null, 1, 14)).Verifiable();

            TSqlFragment fragment = ScriptDomFacade.Parse(sql);
            _ = SqlMarkupReader.ReadHeader(fragment, null, logger.Object);

            logger.Verify();
        }
    }
}