-- @Name EmptyWithParams
CREATE PROCEDURE [dbo].[dbx_tests_syntax_empty_params] 
	@x UNIQUEIDENTIFIER NULL
  , @y BIT
  , @z INT
  , /* @Obfuscate */ @password NVARCHAR(128)
  , /* @ClrType GenericParameterSet */ @ids [dbo].[dbx_codeanalysis_udt_generic] READONLY
AS
	;