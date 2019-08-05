CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_025]
AS
  CREATE TABLE [dbo].[dbx_codeanalysis_error_025_table]
  (
      [a] INT NOT NULL 
	, [b] INT NOT NULL 
	, [c] INT
	, CONSTRAINT [PK_dbx_codeanalysis_error_025_table] PRIMARY KEY ([a], [b])
  )

  CREATE INDEX [IX_dbx_codeanalysis_error_025_table_fail] ON [dbo].[dbx_codeanalysis_error_025_table] ([a])
  CREATE NONCLUSTERED INDEX [IX_dbx_codeanalysis_error_025_table_success1] ON [dbo].[dbx_codeanalysis_error_025_table] ([a])
  CREATE CLUSTERED INDEX [IX_dbx_codeanalysis_error_025_table_success2] ON [dbo].[dbx_codeanalysis_error_025_table] ([a])

  CREATE TABLE [dbo].[dbx_codeanalysis_error_025_table_fail]
  (
      [a] INT NOT NULL 
	, [b] INT NOT NULL 
	, [c] INT -- Implicit NOT NULL
	, CONSTRAINT [PK_dbx_codeanalysis_error_025_table_fail] PRIMARY KEY ([a], [b], [c])
  )
  GO
  CREATE TYPE [dbo].[dbx_codeanalysis_udt_error_025_fail] AS TABLE
  (
      [a] INT NOT NULL 
	, [b] INT NOT NULL 
	, [c] INT -- Implicit NOT NULL
	, PRIMARY KEY ([a], [b], [c])
  )