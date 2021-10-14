-- @Namespace Extension.Primitive
-- @Name GetSinglePrimitiveResult
-- @Return ClrTypes:System.Guid Mode:Single
CREATE PROCEDURE [dbo].[dbx_tests_syntax_singleprimitiveresult]
AS
	SELECT NEWID()