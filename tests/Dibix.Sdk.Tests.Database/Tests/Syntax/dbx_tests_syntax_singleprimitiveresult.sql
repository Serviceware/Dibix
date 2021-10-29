-- @Namespace Extension.Primitive
-- @Name GetSinglePrimitiveResult
-- @Return ClrTypes:uuid Mode:Single
CREATE PROCEDURE [dbo].[dbx_tests_syntax_singleprimitiveresult]
AS
	SELECT NEWID()