CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_025]
AS
  CREATE TABLE [dbo].[dbx_codeanalysis_error_025_table]
  (
      [id] INT NOT NULL CONSTRAINT [PK_dbx_codeanalysis_error_025_table] PRIMARY KEY
  )

  CREATE INDEX [IX_dbx_codeanalysis_error_025_table_fail] ON [dbo].[dbx_codeanalysis_error_025_table] ([id])
  CREATE NONCLUSTERED INDEX [IX_dbx_codeanalysis_error_025_table_success1] ON [dbo].[dbx_codeanalysis_error_025_table] ([id])
  CREATE CLUSTERED INDEX [IX_dbx_codeanalysis_error_025_table_success2] ON [dbo].[dbx_codeanalysis_error_025_table] ([id])

  CREATE TABLE [dbo].[dbx_codeanalysis_error_025_table_fail]
  (
      [id] INT -- Implicit NOT NULL
    , CONSTRAINT [PK_dbx_codeanalysis_error_025_fail] PRIMARY KEY ([id])
  )
  GO
  CREATE TYPE [dbo].[dbx_codeanalysis_error_025_type_fail] AS TABLE
  (
      [id] INT -- Implicit NOT NULL
    , PRIMARY KEY ([id])
  )