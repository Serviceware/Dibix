-- @Namespace Grid
-- @Name GetGrid
-- @Return ClrTypes:GenericContract ResultType:JointContract Name:Items
-- @Return ClrTypes:AccessRights Mode:Single Name:AccessRights
CREATE PROCEDURE [dbo].[dbx_tests_syntax_gridresult_projection]
AS
	SELECT [id] = 1
	UNION ALL
	SELECT [id] = 2

	SELECT [accessrights] = 1