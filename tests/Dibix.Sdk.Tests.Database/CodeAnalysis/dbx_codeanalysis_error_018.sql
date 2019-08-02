CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_018]
AS
  DECLARE @x [dbo].[dbx_codeanalysis_udt_generic]

  UPDATE [dbo].[dbx_table] SET [id] = 1
  DELETE [dbo].[dbx_table]
  
  UPDATE [x] SET [x].[id] = 1 FROM @x AS [x]
  UPDATE @x SET [id] = 1
  UPDATE [dbo].[dbx_table] SET [id] = 1 WHERE [id] = 1
  DELETE @x
  DELETE [dbo].[dbx_table] WHERE [id] = 1