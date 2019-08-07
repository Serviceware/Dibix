CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_027]
AS
  CREATE TABLE [dbo].[dbx_codeanalysis_error_027_table]
  (
    [a] INT NOT NULL CONSTRAINT [PK_dbx_codeanalysis_error_027_table] PRIMARY KEY
  , [b] INT NULL
  , [c] INT NULL
  , [d] INT NULL
  )

  CREATE UNIQUE NONCLUSTERED INDEX [UQ_dbx_codeanalysis_error_027_table_id_success_a] ON [dbo].[dbx_codeanalysis_error_027_table] ([b]) INCLUDE ([c])
  CREATE UNIQUE NONCLUSTERED INDEX [UQ_dbx_codeanalysis_error_027_table_id_success_b] ON [dbo].[dbx_codeanalysis_error_027_table] ([c]) WHERE ([c] > 0)
  CREATE UNIQUE NONCLUSTERED INDEX [UQ_dbx_codeanalysis_error_027_table_id_fail] ON [dbo].[dbx_codeanalysis_error_027_table] ([d])