-- @Namespace Grid
-- @Name GetGrid
-- @Return ClrTypes:GenericContract;Direction;AccessRights SplitOn:direction,accessrights ResultType:JointContract Name:Items
-- @Return ClrTypes:AccessRights Mode:Single Name:AccessRights
CREATE PROCEDURE [dbo].[dbx_tests_syntax_gridresult_projection]
AS
	SELECT [id] = 1, [name] = NULL, [parentid] = NULL, [role] = NULL, [creationtime] = NULL, [imageurl]= NULL, [direction] = 0, [accessrights] = 1
	UNION ALL
	SELECT [id] = 2, [name] = NULL, [parentid] = NULL, [role] = NULL, [creationtime] = NULL, [imageurl]= NULL, [direction] = 1, [accessrights] = 1

	SELECT [accessrights] = 1