-- @Return ClrTypes:string Name:A
-- @Return ClrTypes:int? Name:B
-- @Return ClrTypes:Dibix.Sdk.Tests.CodeGeneration.Direction Name:C
-- @Return ClrTypes:Dibix.Sdk.Tests.CodeGeneration.Direction? Name:D
-- @Return ClrTypes:Dibix.Sdk.Tests.CodeGeneration.Direction,Dibix.Sdk.Tests Name:E
-- @Return ClrTypes:Dibix.Sdk.Tests.CodeGeneration.Direction?,Dibix.Sdk.Tests Name:F
-- @Return ClrTypes:string;int?;Dibix.Sdk.Tests.CodeGeneration.Direction Name:G SplitOn:x,x Converter:ParserTestUtility.Map
-- @Return ClrTypes:Dibix.Sdk.Tests.CodeGeneration.Direction?;Dibix.Sdk.Tests.CodeGeneration.Direction,Dibix.Sdk.Tests;Dibix.Sdk.Tests.CodeGeneration.Direction?,Dibix.Sdk.Tests Name:H SplitOn:x,x Converter:ParserTestUtility.Map
CREATE PROCEDURE [dbo].[dbx_tests_parser_typenames]
AS
	SELECT [x] = 0
	SELECT [x] = 0
	SELECT [x] = 0
	SELECT [x] = 0
	SELECT [x] = 0
	SELECT [x] = 0
	SELECT [x] = 0, [x] = 0, [x] = 0
	SELECT [x] = 0, [x] = 0, [x] = 0
