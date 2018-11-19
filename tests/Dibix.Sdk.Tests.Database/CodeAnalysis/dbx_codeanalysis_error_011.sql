CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_011]
AS
	DECLARE @a [dbo].[dbx_udt]
	DECLARE @b dbo.dbx_udt
	DECLARE @c dbo.[dbx_udt]
	DECLARE @d [dbo].dbx_udt

	SELECT [id]
	FROM dbo.dbx_table

	SELECT [id]
	FROM dbo.[dbx_table]

	SELECT [id]
	FROM [dbo].dbx_table

	SELECT [id]
	FROM [dbo].[dbx_table]
