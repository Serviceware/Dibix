-- @Name FileUpload
CREATE PROCEDURE [dbo].[dbx_tests_syntax_fileupload] /* @ClrType stream */ @data BINARY
AS
	PRINT DATALENGTH(@data)