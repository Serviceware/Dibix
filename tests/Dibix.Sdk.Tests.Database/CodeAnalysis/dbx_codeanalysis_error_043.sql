CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_043_needed1]
AS
BEGIN
    INSERT INTO [dbo].[dbx_table]([id])
                          VALUES (1)
    UPDATE [dbo].[dbx_anothertable] SET [name] = N'x' WHERE [id] = 1
END
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_043_needed2]
AS
BEGIN
    MERGE [dbo].[dbx_anothertable] AS [T]
	USING (VALUES (1, N'x')) AS [S]([id], [name])
	ON ([T].[id] = [S].[id])
	WHEN NOT MATCHED BY SOURCE THEN
		DELETE
	;

	DELETE FROM [dbo].[dbx_table] WHERE [id] = 1
END
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_043_notneeded]
AS
BEGIN
	DELETE FROM [dbo].[dbx_table] WHERE [id] = 1
	
	DECLARE @table TABLE ([id] INT NOT NULL, PRIMARY KEY([id]))
	INSERT INTO @table ([id])
	            VALUES (1)
END
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_043_missingtransaction]
AS
BEGIN
	BEGIN TRANSACTION
		MERGE [dbo].[dbx_anothertable] AS [T]
		USING (VALUES (1, N'x')) AS [S]([id], [name])
		ON ([T].[id] = [S].[id])
		WHEN NOT MATCHED BY SOURCE THEN
			DELETE
		;
	COMMIT

    INSERT INTO [dbo].[dbx_table]([id])
                          VALUES (1)
    UPDATE [dbo].[dbx_anothertable] SET [name] = N'x' WHERE [id] = 1
END
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_043_valid]
AS
BEGIN
	SET XACT_ABORT ON

    MERGE [dbo].[dbx_anothertable] AS [T]
	USING (VALUES (1, N'x')) AS [S]([id], [name])
	ON ([T].[id] = [S].[id])
	WHEN NOT MATCHED BY SOURCE THEN
		DELETE
	;

	DELETE FROM [dbo].[dbx_table] WHERE [id] = 1
END
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_043_alsovalid]
AS
BEGIN
	BEGIN TRANSACTION
		INSERT INTO [dbo].[dbx_table]([id])
                              VALUES (1)

        BEGIN TRANSACTION
			MERGE [dbo].[dbx_anothertable] AS [T]
			USING (VALUES (1, N'x')) AS [S]([id], [name])
			ON ([T].[id] = [S].[id])
			WHEN NOT MATCHED BY SOURCE THEN
				DELETE
			;

			DELETE FROM [dbo].[dbx_table] WHERE [id] = 1
		COMMIT
	COMMIT
END