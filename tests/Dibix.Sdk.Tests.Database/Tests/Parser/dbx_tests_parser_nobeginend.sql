-- @Return ClrTypes:int32 Name:A
-- @Return ClrTypes:int32 Name:B Mode:Single
-- @Return ClrTypes:int32 Name:C
CREATE PROCEDURE [dbo].[dbx_tests_parser_nobeginend] /* @ClrType Dibix.Sdk.VisualStudio.Tests.Direction */ @param1 INT NULL = NULL
AS
	SET NOCOUNT ON

	-- Hi there
	SELECT 1

	SELECT @param1

    SELECT 2