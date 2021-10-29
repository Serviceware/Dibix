-- @Return int32
CREATE PROCEDURE [dbo].[dbx_tests_parser_unionreturn]
AS
	(
		SELECT 1
	)
	UNION ALL
	(
		SELECT 2
	)