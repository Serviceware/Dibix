-- @Return ClrTypes:GenericContract;Direction SplitOn:direction Mode:Single ResultType:JointContract
CREATE PROCEDURE [dbo].[dbx_tests_syntax_singlemultimapresult_projection]
AS
	SELECT [id] = 1, [direction] = 0
	UNION ALL
	SELECT [id] = 1, [direction] = 1