-- 1. Default for nullable constraint
CREATE TABLE [dbo].[dbx_codeanalysis_error_025_table_success1]
(
    [a] INT NOT NULL 
  , [b] INT NULL 
  , [c] AS 1 PERSISTED -- NOT NULL can be explictly specified, however NULL cannot be specified and will be determined by SQL server based on the expression
  , CONSTRAINT [PK_dbx_codeanalysis_error_025_table_success1] PRIMARY KEY ([a], [c])
)
GO
CREATE TABLE [dbo].[dbx_codeanalysis_error_025_table_fail]
(
    [a] INT -- Implicit NOT NULL, because it's included in the PK
  , [b] INT -- Implicit NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_025_table_fail] PRIMARY KEY ([a])
)
GO

-- 2. Default for index clustering
CREATE TABLE [dbo].[dbx_codeanalysis_error_025_table_success2]
(
    [a] INT NOT NULL 
)
GO
CREATE INDEX [IX_dbx_codeanalysis_error_025_table_fail_b] ON [dbo].[dbx_codeanalysis_error_025_table_fail] ([b])
GO
CREATE NONCLUSTERED INDEX [IX_dbx_codeanalysis_error_025_table_success1_b] ON [dbo].[dbx_codeanalysis_error_025_table_success1] ([b])
GO
CREATE CLUSTERED INDEX [IX_dbx_codeanalysis_error_025_table_success2_a] ON [dbo].[dbx_codeanalysis_error_025_table_success2] ([a])