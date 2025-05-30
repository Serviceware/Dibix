﻿-- @Name EmptyWithParamsAndComplexUdt
CREATE PROCEDURE [dbo].[dbx_tests_syntax_empty_params_udt]
    @a NVARCHAR(50)
  , @b NVARCHAR(50)
  , @c UNIQUEIDENTIFIER NULL
  , /* @Obfuscate */ @password NVARCHAR(128)
  , @ids [dbo].[dbx_codeanalysis_udt_generic] READONLY
  , @nested [dbo].[dbx_codeanalysis_udt_inttwo] READONLY
  , @primitivenested [dbo].[dbx_codeanalysis_udt_inttwo] READONLY
  , @d NVARCHAR(50) NULL = NULL
  , @e BIT = 1
  , /* @ClrType Direction */ @f INT NULL = NULL
  , @g NVARCHAR(50) NULL = N'Cake'
AS
	PRINT CONCAT(@a, @b, @c, @password, @ids, @d, @e, @f, @g)