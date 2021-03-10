-- @Name FileResult
-- @FileResult
CREATE PROCEDURE [dbo].[dbx_tests_syntax_fileresult] @id INT
AS
	SELECT [type] = N'png'
	     , [data] = 0x0
	WHERE @id = 1