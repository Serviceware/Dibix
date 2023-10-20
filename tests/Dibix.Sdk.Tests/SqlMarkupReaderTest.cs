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
-- @Return ClrTypes:x;y;z SplitOn:a,b
 --  @Return Mode:Single y
-- @Async
-- @Cake
/* @MergeGridResult */
/* @me: HOW TO TEST IT
   me HOW TO TEST IT?!
 */ 
CREATE PROCEDURE [dbo].[sp] /*  @ClrType Dibix.Sdk.VisualStudio.Tests.Direction  */   @param1 INT
AS
    ;";
            Mock<ILogger> logger = new Mock<ILogger>(MockBehavior.Strict);

            logger.Setup(x => x.LogMessage(LogCategory.Error, null, null, "Unexpected markup element 'Cake'", "source", 5, 4)).Verifiable();

            TSqlFragment fragment = ParseAndExtractProcedureStatement(sql);
            ISqlMarkupDeclaration map = SqlMarkupReader.Read(fragment, SqlMarkupCommentKind.SingleLine, source: "source", logger.Object);

            logger.Verify();

            Assert.IsTrue(map.TryGetSingleElement("Namespace", null, null, out ISqlElement element));
            Assert.AreEqual(1, element.Location.Line);
            Assert.AreEqual(4, element.Location.Column);
            Assert.IsTrue(map.TryGetSingleElementValue("Namespace", null, null, out string value));
            Assert.AreEqual("X", value);

            IList<ISqlElement> returnElements = map.GetElements("Return").ToArray();
            Assert.AreEqual(2, returnElements.Count);
            Assert.IsTrue(returnElements[0].TryGetPropertyValue("ClrTypes", isDefault: true, out Token<string> elementValue));
            Assert.AreEqual("x;y;z", elementValue.Value);
            Assert.AreEqual(2, elementValue.Location.Line);
            Assert.AreEqual(21, elementValue.Location.Column);
            Assert.IsTrue(returnElements[0].TryGetPropertyValue("SplitOn", isDefault: false, out elementValue));
            Assert.AreEqual("a,b", elementValue.Value);
            Assert.AreEqual(2, elementValue.Location.Line);
            Assert.AreEqual(35, elementValue.Location.Column);
            Assert.IsTrue(returnElements[1].TryGetPropertyValue("ClrTypes", isDefault: true, out elementValue));
            Assert.AreEqual("y", elementValue.Value);
            Assert.AreEqual(3, elementValue.Location.Line);
            Assert.AreEqual(26, elementValue.Location.Column);
            Assert.IsTrue(returnElements[1].TryGetPropertyValue("Mode", isDefault: false, out elementValue));
            Assert.AreEqual("Single", elementValue.Value);
            Assert.AreEqual(3, elementValue.Location.Line);
            Assert.AreEqual(19, elementValue.Location.Column);

            Assert.IsTrue(map.HasSingleElement("Async", null, null));

            Assert.IsFalse(map.HasSingleElement("MergeGridResult", null, null));

            ProcedureParameter parameter = ((ProcedureStatementBodyBase)fragment).Parameters[0];
            map = SqlMarkupReader.Read(parameter, SqlMarkupCommentKind.MultiLine, source: String.Empty, logger: null);
            Assert.IsTrue(map.TryGetSingleElementValue("ClrType", null, null, out elementValue));
            Assert.AreEqual("Dibix.Sdk.VisualStudio.Tests.Direction", elementValue.Value);
            Assert.AreEqual(10, elementValue.Location.Line);
            Assert.AreEqual(42, elementValue.Location.Column);
        }

        [TestMethod]
        public void MissingPropertyValue_LogsError()
        {
            const string sql = @"-- @Return Name:
CREATE PROCEDURE [dbo].[sp]
AS
    ;";
            
            Mock<ILogger> logger = new Mock<ILogger>(MockBehavior.Strict);

            logger.Setup(x => x.LogMessage(LogCategory.Error, null, null, "Missing value for 'Name' property", String.Empty, 1, 12)).Verifiable();

            TSqlFragment fragment = ParseAndExtractProcedureStatement(sql);
            _ = SqlMarkupReader.Read(fragment, SqlMarkupCommentKind.SingleLine, source: String.Empty, logger.Object);

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

            logger.Setup(x => x.LogMessage(LogCategory.Error, null, null, "Duplicate property for @Return.ClrTypes", String.Empty, 1, 23)).Verifiable();

            TSqlFragment fragment = ParseAndExtractProcedureStatement(sql);
            _ = SqlMarkupReader.Read(fragment, SqlMarkupCommentKind.SingleLine, source: String.Empty, logger.Object);

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

            logger.Setup(x => x.LogMessage(LogCategory.Error, null, null, "Multiple default properties specified for @Return", String.Empty, 1, 14)).Verifiable();

            TSqlFragment fragment = ParseAndExtractProcedureStatement(sql);
            _ = SqlMarkupReader.Read(fragment, SqlMarkupCommentKind.SingleLine, source: String.Empty, logger.Object);

            logger.Verify();
        }

        private static TSqlStatement ParseAndExtractProcedureStatement(string sql) => ((TSqlScript)ScriptDomFacade.Parse(sql)).Batches.Single().Statements.Single();
    }
}