CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_025]
AS
  -- 1. Default for nullable constraint
  CREATE TABLE [dbo].[dbx_codeanalysis_error_025_table_success]
  (
      [a] INT NOT NULL 
	, [b] INT NULL 
	, [c] AS 1 PERSISTED
	, CONSTRAINT [PK_dbx_codeanalysis_error_025_table_success] PRIMARY KEY ([a])
  )

  CREATE TABLE [dbo].[dbx_codeanalysis_error_025_table_fail]
  (
      [a] INT -- Implicit NOT NULL, because it's included in the PK
	, [b] INT -- Implicit NULL
	, [c] INT NULL
	, CONSTRAINT [PK_dbx_codeanalysis_error_025_table_fail] PRIMARY KEY ([a])
  )

  -- 2. Default for index clustering
  CREATE INDEX [IX_dbx_codeanalysis_error_025_table_fail_b] ON [dbo].[dbx_codeanalysis_error_025_table_fail] ([b])
  CREATE NONCLUSTERED INDEX [IX_dbx_codeanalysis_error_025_table_success_b] ON [dbo].[dbx_codeanalysis_error_025_table_success] ([b])
  CREATE CLUSTERED INDEX [IX_dbx_codeanalysis_error_025_table_success_c] ON [dbo].[dbx_codeanalysis_error_025_table_success] ([c])