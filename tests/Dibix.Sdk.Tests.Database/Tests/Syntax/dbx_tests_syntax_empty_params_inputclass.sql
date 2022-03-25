-- @Name EmptyWithParams
-- @GenerateInputClass
CREATE PROCEDURE [dbo].[dbx_tests_syntax_empty_params_inputclass]
	@x UNIQUEIDENTIFIER NULL
  , /* @Obfuscate */ @password NVARCHAR(128)
  , @ids [dbo].[dbx_codeanalysis_udt_generic] READONLY
  , @y BIT OUTPUT
  , @z INT
AS
	PRINT CONCAT(@x, @password, @ids, @y, @z)
	SET @y = NULL