-- @Name ETagError
-- @Return ClrTypes:ETagContractError Mode:Single
CREATE PROCEDURE [dbo].[dbx_tests_syntax_etag_error] @id INT, @ifnonematch NVARCHAR(50) = NULL
AS
BEGIN
	IF @ifnonematch IS NOT NULL
		THROW 304001, @ifnonematch, 1

	SELECT [id] = @id, [version] = CONVERT(BINARY(8), @id), [name] = N'Name'
END