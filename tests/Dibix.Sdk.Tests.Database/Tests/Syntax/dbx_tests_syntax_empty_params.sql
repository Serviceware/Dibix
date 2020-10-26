-- @Name EmptyWithParams
-- DROP PROCEDURE [dbo].[dbx_tests_syntax_empty_params]
CREATE PROCEDURE [dbo].[dbx_tests_syntax_empty_params] 
	@x UNIQUEIDENTIFIER NULL
  , /* @Obfuscate */ @password NVARCHAR(128)
  , @ids [dbo].[dbx_codeanalysis_udt_generic] READONLY
  , @y BIT = 1
  , @z INT NULL = NULL
AS
	;