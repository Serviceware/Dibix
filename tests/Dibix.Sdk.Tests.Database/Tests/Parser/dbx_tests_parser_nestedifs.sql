-- @Return int Name:A
-- @Return int Name:B
CREATE PROCEDURE [dbo].[dbx_tests_parser_nestedifs]
AS
	IF 0 = 1
	BEGIN
		IF 1 = 0
			SELECT 1.0 AS [action]
		ELSE
			SELECT 1.1 AS [action]
	END
	ELSE IF 0 = 1
	BEGIN
		DECLARE @x INT = 1
		SELECT 2 AS [action]
	END
	ELSE
	BEGIN
		SELECT 3 AS [action]
	END

	
	IF 0 = 1
		SELECT 1
	ELSE IF 0 = 2
		SELECT 2
	ELSE
		SELECT 3