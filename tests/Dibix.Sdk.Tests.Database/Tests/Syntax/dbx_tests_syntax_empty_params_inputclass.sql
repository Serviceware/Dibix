-- @Name EmptyWithParams
-- @GenerateInputClass
CREATE PROCEDURE [dbo].[dbx_tests_syntax_empty_params_inputclass]
	/* @Nullable */ @x UNIQUEIDENTIFIER
--, /* @Obfuscate */ @password NVARCHAR(128)
  , /* @ClrType GenericParameterSet */ @ids [dbo].[dbx_codeanalysis_udt_generic] READONLY
AS
	;