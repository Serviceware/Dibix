-- @Namespace Extension.Primitive
-- @Name GetSinglePrimitiveResult
-- @Return ClrTypes:uuid Mode:Single Wtf:IsThis
CREATE PROCEDURE [dbo].[dbx_tests_syntax_singleprimitiveresult_invalidreturnproperty]
AS
	SELECT NEWID()