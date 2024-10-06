CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_010]
AS
	SELECT [id]
	FROM [dbo].[dbx_table]
	
	SELECT [dbo].[dbx_table].[id]
	FROM [dbo].[dbx_table]
	INNER JOIN [dbo].[dbx_anothertable] ON [dbo].[dbx_table].[id] = [dbo].[dbx_anothertable].[id]
	
	SELECT [dbo].[dbx_table].[id]
	FROM [dbo].[dbx_table]
	CROSS APPLY (
		SELECT [dbo].[dbx_anothertable].[id]
		FROM [dbo].[dbx_anothertable] 
		WHERE [dbo].[dbx_anothertable].[id] = [dbo].[dbx_table].[id]
	) AS [x]

	SELECT [a].[id]
	FROM [dbo].[dbx_table] AS [a]
	INNER JOIN [dbo].[dbx_anothertable] AS [b] ON [b].[id] = [a].[id]

	/*SELECT [idx]
	FROM [dbo].[dbx_table] AS [a]
	INNER JOIN [dbo].[dbx_anothertable] AS [b] ON ISNULL([idx], 0) = [a].[id]*/