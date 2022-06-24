-- @Namespace Grid
-- @Name GetGrid
-- @Return ClrTypes:Extension.MultiMapContract;GenericContract;Direction SplitOn:id,direction Mode:Single Name:Item
-- @Return ClrTypes:Direction Name:Directions
-- @ResultTypeName Grid.GridResult
CREATE PROCEDURE [dbo].[dbx_tests_syntax_gridresult_existingresultcontract]
AS
	SELECT [x] = N'527B8008-AE6E-421F-91B2-5A0583070BCD', [id] = 1, [name] = NULL, [parentid] = NULL, [role] = NULL, [creationtime] = NULL, [imageurl]= NULL, [direction] = 0
	UNION ALL
	SELECT [x] = N'527B8008-AE6E-421F-91B2-5A0583070BCD', [id] = 2, [name] = NULL, [parentid] = NULL, [role] = NULL, [creationtime] = NULL, [imageurl]= NULL, [direction] = 0

	SELECT 1