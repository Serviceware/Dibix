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

	SELECT [t].[id]
	FROM [dbo].[dbx_table] AS [t]

	SELECT t.[id]
	FROM [dbo].[dbx_table] AS [t]

	SELECT [t].id
	FROM [dbo].[dbx_table] AS [t]

	SELECT S.[id]
	FROM [dbo].[dbx_table] AS [S]

	SELECT T.[id]
	FROM [dbo].[dbx_table] AS [T]
