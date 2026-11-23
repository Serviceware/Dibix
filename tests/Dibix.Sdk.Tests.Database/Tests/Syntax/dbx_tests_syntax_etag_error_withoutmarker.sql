-- @Name ETagErrorWithoutMarker
-- @Return ClrTypes:ETagContractWithoutMarker Mode:Single
CREATE PROCEDURE [dbo].[dbx_tests_syntax_etag_error_withoutmarker] @id INT, @ifnonematch NVARCHAR(50) = NULL
AS
BEGIN
	IF @ifnonematch IS NOT NULL
		THROW 304000, @ifnonematch, 1

	SELECT [id] = @id, [etag] = N'1'
END