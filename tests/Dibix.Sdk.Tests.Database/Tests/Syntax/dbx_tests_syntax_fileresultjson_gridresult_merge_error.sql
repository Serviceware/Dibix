-- @Name FileResultJsonMergeGridResultError
-- @Return ClrTypes:JsonFileResultContractError Mode:SingleOrDefault
-- @Return ClrTypes:Direction Name:Directions
-- @MergeGridResult
-- @FileResult Json
CREATE PROCEDURE [dbo].[dbx_tests_syntax_fileresultjson_gridresult_merge_error]
AS
BEGIN
	SELECT [x] = N'527B8008-AE6E-421F-91B2-5A0583070BCD', [filename] = N'the_file_result.json'

	SELECT 1
END