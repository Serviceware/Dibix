-- @Return ClrTypes:int Name:A
-- @Return ClrTypes:int Name:B Mode:Single
-- @Return ClrTypes:int Name:C
CREATE PROCEDURE [dbo].[dbx_tests_parser_nobeginend] /* @ClrType Dibix.Sdk.Tests.CodeGeneration.Direction */ @param1 INT
AS
	SET NOCOUNT ON

	-- Hi there
	SELECT 1

	SELECT @param1

    SELECT 2