-- @Return ClrTypes:string Name:A
-- @Return ClrTypes:int? Name:B
-- @Return ClrTypes:Dibix.Sdk.VisualStudio.Tests.Direction Name:C
-- @Return ClrTypes:Dibix.Sdk.VisualStudio.Tests.Direction? Name:D
-- @Return ClrTypes:Dibix.Sdk.VisualStudio.Tests.Direction,Dibix.Sdk.VisualStudio.Tests Name:E
-- @Return ClrTypes:Dibix.Sdk.VisualStudio.Tests.Direction?,Dibix.Sdk.VisualStudio.Tests Name:F
-- @Return ClrTypes:string;int?;Dibix.Sdk.VisualStudio.Tests.Direction Name:G SplitOn:x,x Converter:ParserTestUtility.Map
-- @Return ClrTypes:Dibix.Sdk.VisualStudio.Tests.Direction?;Dibix.Sdk.VisualStudio.Tests.Direction,Dibix.Sdk.VisualStudio.Tests;Dibix.Sdk.VisualStudio.Tests.Direction?,Dibix.Sdk.VisualStudio.Tests Name:H SplitOn:x,x Converter:ParserTestUtility.Map
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
