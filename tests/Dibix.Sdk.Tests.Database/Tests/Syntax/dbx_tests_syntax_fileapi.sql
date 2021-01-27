-- @FileApi
CREATE PROCEDURE [dbo].[dbx_tests_syntax_fileapi] @id INT
AS
	SELECT [type] = N'png'
	     , [data] = 0x0
	WHERE @id = 1