CREATE PROCEDURE [dbo].[dbx_lint_error_006]
AS
	SELECT [id] AS [id]
	FROM [dbo].[dbx_table]

	SELECT [id] AS [idx]
	FROM [dbo].[dbx_table]

	SELECT 1 AS [id], [id]
	FROM [dbo].[dbx_table]