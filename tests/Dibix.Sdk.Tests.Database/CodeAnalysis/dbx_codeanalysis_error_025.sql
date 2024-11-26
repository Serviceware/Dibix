-- 1. Default for nullable constraint
CREATE TABLE [dbo].[dbx_codeanalysis_error_025_table_success1]
(
    [a] INT NOT NULL 
  , [b] INT NULL 
  , [c] AS 1 PERSISTED -- For computed persisted columns, NOT NULL can be explictly specified, however NULL cannot be specified and will be determined by SQL server based on the expression.
  , [d] AS 1           -- Neither NOT NULL nor NULL can be explicitly specified for non persisted computed columns.
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
GO

-- 3. Default JOIN type
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_025]
AS
    SELECT [x].[id]
    FROM (VALUES (1)) AS [x]([id])
    JOIN (VALUES (1)) AS [y]([id]) ON [x].[id] = [y].[id]       -- Implicit INNER JOIN => Fail
    INNER JOIN (VALUES (1)) AS [z]([id]) ON [x].[id] = [z].[id] -- Explicit INNER JOIN => Success