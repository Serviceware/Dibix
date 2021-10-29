-- @Name EmptyWithOutputParam
-- @Return ClrTypes:int16 Mode:Single
CREATE PROCEDURE [dbo].[dbx_tests_syntax_empty_params_out] @a SMALLINT OUT
AS
	SET @a = 2

	SELECT @a