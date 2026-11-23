-- @Name ETag
-- @Return ClrTypes:ETagContract Mode:Single
CREATE PROCEDURE [dbo].[dbx_tests_syntax_etag] @id INT, @ifnonematch NVARCHAR(50) = NULL
AS
BEGIN
	DECLARE @etag NVARCHAR(50) = N'2af35be0'

	IF @ifnonematch = @etag
		THROW 304000, @etag, 1

	SELECT [id] = @id, [etag] = @etag, [name] = N'Name'
END