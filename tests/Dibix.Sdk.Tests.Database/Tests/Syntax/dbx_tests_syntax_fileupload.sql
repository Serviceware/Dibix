-- @Name FileUpload
-- @Async
CREATE PROCEDURE [dbo].[dbx_tests_syntax_fileupload] /* @ClrType stream */ @data BINARY(1)
AS
	PRINT DATALENGTH(@data)