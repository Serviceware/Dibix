CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_027]
AS
  CREATE TABLE [dbo].[dbx_codeanalysis_error_027_table]
  (
    [id] INT NOT NULL CONSTRAINT [PK_dbx_codeanalysis_error_027_table] PRIMARY KEY
  , INDEX [UQ_dbx_codeanalysis_error_027_table_id_success1_1] UNIQUE ([id]) INCLUDE ([id])
  , INDEX [UQ_dbx_codeanalysis_error_027_table_id_success1_2] UNIQUE ([id]) WHERE ([id] > 0)
  , INDEX [UQ_dbx_codeanalysis_error_027_table_id_fail1] UNIQUE ([id])
  )

  CREATE UNIQUE NONCLUSTERED INDEX [UQ_dbx_codeanalysis_error_027_table_id_success2_1] ON [dbo].[dbx_codeanalysis_error_027_table] ([id]) INCLUDE ([id])
  CREATE UNIQUE NONCLUSTERED INDEX [UQ_dbx_codeanalysis_error_027_table_id_success2_2] ON [dbo].[dbx_codeanalysis_error_027_table] ([id]) WHERE ([id] > 0)
  CREATE UNIQUE NONCLUSTERED INDEX [UQ_dbx_codeanalysis_error_027_table_id_fail2] ON [dbo].[dbx_codeanalysis_error_027_table] ([id])