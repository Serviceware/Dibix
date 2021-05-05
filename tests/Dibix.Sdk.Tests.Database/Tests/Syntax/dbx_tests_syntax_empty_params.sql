-- @Name EmptyWithParams
CREATE PROCEDURE [dbo].[dbx_tests_syntax_empty_params]
    @u NVARCHAR(50)
  , @v NVARCHAR(50)
  , @w UNIQUEIDENTIFIER NULL
  , /* @Obfuscate */ @password NVARCHAR(128)
  , @ids [dbo].[dbx_codeanalysis_udt_generic] READONLY
  , @x NVARCHAR(50) = NULL
  , @y BIT = 1
  , @z INT NULL = NULL
AS
    ;