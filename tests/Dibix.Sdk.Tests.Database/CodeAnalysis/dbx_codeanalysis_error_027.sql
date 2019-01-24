CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_027]
AS
  CREATE TABLE [dbo].[dbx_codeanalysis_error_027_table]
  (
    [id] INT NOT NULL CONSTRAINT [PK_dbx_codeanalysis_error_027_table] PRIMARY KEY
  )

  CREATE UNIQUE NONCLUSTERED INDEX [UQ_dbx_codeanalysis_error_027_table_id_success1] ON [dbo].[dbx_codeanalysis_error_027_table] ([id]) INCLUDE ([id])
  CREATE UNIQUE NONCLUSTERED INDEX [UQ_dbx_codeanalysis_error_027_table_id_success2] ON [dbo].[dbx_codeanalysis_error_027_table] ([id]) WHERE ([id] > 0)
  CREATE UNIQUE NONCLUSTERED INDEX [UQ_dbx_codeanalysis_error_027_table_id_fail] ON [dbo].[dbx_codeanalysis_error_027_table] ([id])