-- @Name EmptyWithParams
CREATE PROCEDURE [dbo].[dbx_tests_syntax_empty_params]
    @a NVARCHAR(50)
  , @b NVARCHAR(50)
  , @c UNIQUEIDENTIFIER NULL
  , /* @Obfuscate */ @password NVARCHAR(128) NULL
  , @ids [dbo].[dbx_codeanalysis_udt_int] READONLY
  , @d NVARCHAR(50) NULL = NULL
  , @e BIT = 1
  , /* @ClrType Direction */ @f INT NULL = NULL
  , @g NVARCHAR(50) NULL = N'Cake'
AS
	PRINT CONCAT(@a, @b, @c, @password, @ids, @d, @e, @f, @g)