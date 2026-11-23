-- @Name ETagErrorWithoutThrow
-- @Return ClrTypes:ETagContractWithoutThrow Mode:Single
CREATE PROCEDURE [dbo].[dbx_tests_syntax_etag_error_withoutthrow] @id INT
AS
BEGIN
	SELECT [id] = @id, [etag] = N'1'
END