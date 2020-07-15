CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_036]
AS
	MERGE [dbo].[dbx_table] AS [target]
    USING (VALUES(1)) AS [source]([id])
    ON ([target].[id] = [source].[id])
    WHEN MATCHED THEN
        UPDATE SET [target].[id] = [source].[id]
    ;

	UPDATE [dbo].[dbx_table] SET [id] = 1

	DECLARE @table1 TABLE([id] INT NOT NULL, PRIMARY KEY([id]))
	
	DECLARE @table2 TABLE([id] INT NOT NULL)

	DECLARE @table3 [dbo].[dbx_codeanalysis_udt_generic]

	UPDATE @table1 SET [id] = 1 -- Fail

	UPDATE @table2 SET [id] = 1 -- OK

	UPDATE @table3 SET [id] = 1 -- Fail

	UPDATE @table3 SET [name] = N'lol' -- OK

	UPDATE [r] SET [r].[id_current] = [r].[id_new] -- Fail
	FROM (
		SELECT [id_current] = [id], [id_new] = 1
		FROM @table1
	) AS [r]

	UPDATE [r] SET [r].[id_current] = [r].[id_new] -- Fail
	FROM (
		SELECT [id_current] = [id], [id_new] = 1
		FROM @table3
	) AS [r]

	MERGE @table1 AS [T]
	USING (VALUES(1)) AS [S]([id]) ON [T].[id] = [S].[id]
	WHEN MATCHED THEN
		UPDATE SET [T].[id] = [S].[id] -- Fail
	;

	MERGE @table3 AS [T]
	USING (VALUES(1)) AS [S]([id]) ON [T].[id] = [S].[id]
	WHEN MATCHED THEN
		UPDATE SET [T].[id] = [S].[id] -- Fail
	;

	;WITH [x] AS (
		SELECT [idx] = [id]
		FROM @table1
	)
	MERGE [x] AS [T]
	USING (VALUES(1)) AS [S]([id]) ON [T].[idx] = [S].[id]
	WHEN MATCHED THEN
		UPDATE SET [T].[idx] = [S].[id] -- Fail
	;

	;WITH [x] AS (
		SELECT [idx] = [id]
		FROM @table3
	)
	MERGE [x] AS [T]
	USING (VALUES(1)) AS [S]([id]) ON [T].[idx] = [S].[id]
	WHEN MATCHED THEN
		UPDATE SET [T].[idx] = [S].[id] -- Fail
	;