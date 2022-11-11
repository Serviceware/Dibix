CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_008]
AS
	SELECT [x].[id], [x].[id] [idx]
	FROM [dbo].[dbx_table] [x]

	SELECT [x].[id]
	FROM (VALUES (1)) AS [z]([id])
	OUTER APPLY (
		SELECT [x].[id]
		FROM [dbo].[dbx_table] AS [x]
		WHERE [z].[id] = [x].[id]
	) [x]

	SELECT [x].[id] AS [idx]
	FROM [dbo].[dbx_table] AS [x]

	SELECT [idx] = [x].[id]
	FROM [dbo].[dbx_table] AS [x]

	DECLARE @table TABLE ([x] BIT)
	INSERT INTO @table ([x]) VALUES (1)
	INSERT @table ([x]) VALUES (1)
	DELETE FROM @table WHERE [x] = 1
	DELETE [t] FROM @table AS [t] WHERE [x] = 1
	DELETE @table WHERE [x] = 1