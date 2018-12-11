CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_015]
AS
  INSERT INTO [dbo].[dbx_table] ([id]) VALUES (1)
  INSERT INTO [dbo].[dbx_table] VALUES (1)

  MERGE [dbo].[dbx_table] AS [target]
  USING (VALUES (1)) AS [source]([id])
   ON [source].[id] = [target].[id]
  WHEN NOT MATCHED BY TARGET THEN
  	INSERT VALUES([source].[id])
  ;