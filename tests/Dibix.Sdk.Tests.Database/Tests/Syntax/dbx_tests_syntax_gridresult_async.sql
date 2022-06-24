-- @Namespace Grid
-- @Name GetGrid
-- @Async
-- @Return ClrTypes:GenericContract Name:Items
-- @Return ClrTypes:Direction Name:Directions
CREATE PROCEDURE [dbo].[dbx_tests_syntax_gridresult_async]
AS
	SELECT [id]           = 1
         , [name]         = NULL
         , [parentid]     = NULL
         , [role]         = NULL
         , [creationtime] = NULL
         , [imageurl]     = NULL
	UNION ALL
	SELECT [id]           = 2
         , [name]         = NULL
         , [parentid]     = NULL
         , [role]         = NULL
         , [creationtime] = NULL
         , [imageurl]     = NULL

	SELECT 1