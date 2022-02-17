-- @Namespace Extension.Primitive
-- @Name GetSinglePrimitiveResult
-- @Return ClrTypes:uuid Mode:Single Wtf:IsThis
-- @Wtf IsThis
CREATE PROCEDURE [dbo].[dbx_tests_syntax_singleprimitiveresult_invalidmarkup]
AS
	SELECT NEWID()