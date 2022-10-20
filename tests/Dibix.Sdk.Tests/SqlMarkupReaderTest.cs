using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Dibix.Sdk.Tests
{
    [TestClass]
    public sealed class SqlMarkupReaderTest
    {
        [TestMethod]
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
            ISqlMarkupDeclaration map = SqlMarkupReader.ReadHeader(fragment, source: String.Empty, logger: null);

            Assert.IsTrue(map.TryGetSingleElement("Namespace", null, null, out ISqlElement element));
            Assert.AreEqual(1, element.Line);
            Assert.AreEqual(4, element.Column);

            Assert.IsTrue(map.TryGetSingleElementValue("Namespace", null, null, out string value));
            Assert.AreEqual("X", value);

            Assert.IsTrue(map.HasSingleElement("OmitResult", null, null));

            Assert.IsTrue(map.TryGetSingleElement("Cake", null, null, out element));
            Assert.AreEqual(7, element.Line);
            Token<string> prop1 = element.GetPropertyValue("Prop1");
            Assert.AreEqual("true", prop1.Value);
            Assert.AreEqual(7, prop1.Line);
            Assert.AreEqual(13, prop1.Column);
            Token<string> prop2 = element.GetPropertyValue("Prop2");
            Assert.AreEqual("False;But;True", prop2.Value);
            Assert.AreEqual(7, prop2.Line);
            Assert.AreEqual(24, prop2.Column);

            IList<ISqlElement> returnElements = map.GetElements("Return").ToArray();
            Assert.AreEqual(2, returnElements.Count);
            Assert.IsTrue(returnElements[0].TryGetPropertyValue("ClrTypes", isDefault: true, out Token<string> elementValue));
            Assert.AreEqual("x", elementValue.Value);
            Assert.AreEqual(2, elementValue.Line);
            Assert.AreEqual(21, elementValue.Column);
            Assert.IsTrue(returnElements[1].TryGetPropertyValue("ClrTypes", isDefault: true, out elementValue));
            Assert.AreEqual("y", elementValue.Value);
            Assert.AreEqual(3, elementValue.Line);
            Assert.AreEqual(26, elementValue.Column);
            Assert.IsTrue(returnElements[1].TryGetPropertyValue("Mode", isDefault: false, out elementValue));
            Assert.AreEqual(3, elementValue.Line);
            Assert.AreEqual(19, elementValue.Column);

            ProcedureParameter parameter = ((ProcedureStatementBodyBase)((TSqlScript)fragment).Batches[0].Statements[0]).Parameters[0];
            map = SqlMarkupReader.ReadFragment(parameter, source: String.Empty, logger: null);
            Assert.IsTrue(map.TryGetSingleElementValue("ClrType", null, null, out elementValue));
            Assert.AreEqual("Dibix.Sdk.VisualStudio.Tests.Direction", elementValue.Value);
            Assert.AreEqual(9, elementValue.Line);
            Assert.AreEqual(42, elementValue.Column);
        }

        [TestMethod]
        public void MissingPropertyValue_LogsError()
        {
            const string sql = @"-- @Return Name:
CREATE PROCEDURE [dbo].[sp]
AS
    ;";
            
            Mock<ILogger> logger = new Mock<ILogger>(MockBehavior.Strict);

            logger.Setup(x => x.LogError("Missing value for 'Name' property", String.Empty, 1, 12)).Verifiable();

            TSqlFragment fragment = ScriptDomFacade.Parse(sql);
            _ = SqlMarkupReader.ReadHeader(fragment, source: String.Empty, logger.Object);

            logger.Verify();
        }

        [TestMethod]
        public void DuplicateProperty_LogsError()
        {
            const string sql = @"-- @Return ClrTypes:A ClrTypes:B
CREATE PROCEDURE [dbo].[sp]
AS
    ;";
            
            Mock<ILogger> logger = new Mock<ILogger>(MockBehavior.Strict);

            logger.Setup(x => x.LogError("Duplicate property for @Return.ClrTypes", String.Empty, 1, 23)).Verifiable();

            TSqlFragment fragment = ScriptDomFacade.Parse(sql);
            _ = SqlMarkupReader.ReadHeader(fragment, source: String.Empty, logger.Object);

            logger.Verify();
        }

        [TestMethod]
        public void DuplicateDefaultValue_LogsError()
        {
            const string sql = @"-- @Return A B
CREATE PROCEDURE [dbo].[sp]
AS
    ;";

            Mock<ILogger> logger = new Mock<ILogger>(MockBehavior.Strict);

            logger.Setup(x => x.LogError("Multiple default properties specified for @Return", String.Empty, 1, 14)).Verifiable();

            TSqlFragment fragment = ScriptDomFacade.Parse(sql);
            _ = SqlMarkupReader.ReadHeader(fragment, source: String.Empty, logger.Object);

            logger.Verify();
        }
    }
}