-- @Namespace File
-- @Name FileUpload
-- @Async
CREATE PROCEDURE [dbo].[dbx_tests_syntax_fileupload]
    /* @ClrType stream */ @data     BINARY(1)
  ,                       @mimetype NVARCHAR(128) NULL
  ,                       @filename NVARCHAR(510) NULL
AS
BEGIN
	DECLARE @message NVARCHAR(MAX) = CONCAT(DATALENGTH(@data), @mimetype, @filename)
	PRINT @message
END