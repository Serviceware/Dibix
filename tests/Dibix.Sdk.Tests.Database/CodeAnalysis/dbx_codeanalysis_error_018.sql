CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_018]
AS
  UPDATE [dbo].[dbx_table] SET [id] = 1
  DELETE [dbo].[dbx_table]

  UPDATE [dbo].[dbx_table] SET [id] = 1 WHERE [id] = 1
  DELETE [dbo].[dbx_table] WHERE [id] = 1