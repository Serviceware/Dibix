-- @Name FileResultJsonGridResult
-- @Return ClrTypes:string Name:FileName Mode:Single
-- @Return ClrTypes:Direction Name:Directions
-- @FileResult Json
CREATE PROCEDURE [dbo].[dbx_tests_syntax_fileresultjson_gridresult]
AS
BEGIN
    SELECT [filename] = N'the_file_result.json'

	SELECT 1
END