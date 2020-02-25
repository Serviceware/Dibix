-- @Return ClrTypes:Dibix.Sdk.Tests.CodeGeneration.SpecialEntity
CREATE PROCEDURE [dbo].[dbx_tests_syntax_concreteresult_cltrcontract]
AS
	SELECT [id] = 1, [name] = N'Cake', [age] = 12
	UNION ALL
	SELECT [id] = 2, [name] = N'Cookie', [age] = 16