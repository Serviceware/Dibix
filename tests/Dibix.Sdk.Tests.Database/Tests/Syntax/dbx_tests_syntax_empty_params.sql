-- @Name EmptyWithParams
CREATE PROCEDURE [dbo].[dbx_tests_syntax_empty_params]
    @v NVARCHAR(50)
  , @w NVARCHAR(50)
  , @x UNIQUEIDENTIFIER NULL
  , /* @Obfuscate */ @password NVARCHAR(128)
  , @ids [dbo].[dbx_codeanalysis_udt_generic] READONLY
  , @y BIT = 1
  , @z INT NULL = NULL
AS
    ;