-- @Namespace Grid
-- @Name GetGrid
-- @Async
-- @Return ClrTypes:GenericContract Name:Items
-- @Return ClrTypes:Direction Name:Directions
CREATE PROCEDURE [dbo].[dbx_tests_syntax_gridresult_async]
AS
	SELECT [id] = 1
	UNION ALL
	SELECT [id] = 2

	SELECT 1