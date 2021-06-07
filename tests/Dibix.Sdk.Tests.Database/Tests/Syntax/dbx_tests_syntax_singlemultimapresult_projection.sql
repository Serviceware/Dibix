-- @Return ClrTypes:GenericContract;Direction;AccessRights SplitOn:direction,accessrights Mode:Single ResultType:JointContract
CREATE PROCEDURE [dbo].[dbx_tests_syntax_singlemultimapresult_projection]
AS
	SELECT [id] = 1, [direction] = 0, [accessrights] = 1
	UNION ALL
	SELECT [id] = 1, [direction] = 1, [accessrights] = 1