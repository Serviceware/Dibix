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

	UPDATE @table1 SET [id] = 1 -- Fail

	UPDATE @table2 SET [id] = 1 -- OK