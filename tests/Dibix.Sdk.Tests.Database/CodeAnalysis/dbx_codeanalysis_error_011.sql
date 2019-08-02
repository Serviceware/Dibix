CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_011]
AS
	DECLARE @a [dbo].[dbx_codeanalysis_udt_generic]
	DECLARE @b dbo.dbx_codeanalysis_udt_generic
	DECLARE @c dbo.[dbx_codeanalysis_udt_generic]
	DECLARE @d [dbo].dbx_codeanalysis_udt_generic

	SELECT [id]
	FROM dbo.dbx_table

	SELECT [id]
	FROM dbo.[dbx_table]

	SELECT [id]
	FROM [dbo].dbx_table

	SELECT [id]
	FROM [dbo].[dbx_table]
