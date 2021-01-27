-- @Namespace Grid
-- @Name GetGrid
-- @Return ClrTypes:GenericContract;Direction SplitOn:direction ResultType:JointContract Name:Items
-- @Return ClrTypes:AccessRights Mode:Single Name:AccessRights
CREATE PROCEDURE [dbo].[dbx_tests_syntax_gridresult_projection]
AS
	SELECT [id] = 1, [direction] = 0
	UNION ALL
	SELECT [id] = 2, [direction] = 1

	SELECT [accessrights] = 1