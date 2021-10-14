-- @Namespace Extension.Primitive
-- @Name GetSinglePrimitiveResult
-- @Return ClrTypes:uuid Mode:Single
-- @Async
CREATE PROCEDURE [dbo].[dbx_tests_syntax_singleprimitiveresult_async]
AS
	SELECT NEWID()