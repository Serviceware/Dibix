CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_023]
AS
  CREATE TABLE [dbx_codeanalysis_error_023_fail]
  (
      [a] INT
    , [b] NVARCHAR(128)
    , CONSTRAINT [PK_dbx_codeanalysis_error_023_fail] PRIMARY KEY ([a], [b])
    , CONSTRAINT [UQ_dbx_codeanalysis_error_023_fail_b] UNIQUE ([b])
  )

  CREATE TABLE [dbx_codeanalysis_error_023_success] -- PK = FK
  (
      [b] NVARCHAR(128)
    , CONSTRAINT [PK_dbx_codeanalysis_error_023_fail] PRIMARY KEY ([b])
    , CONSTRAINT [FK_dbx_codeanalysis_error_023_fail_b] FOREIGN KEY ([b]) REFERENCES [dbo].[dbx_codeanalysis_error_023_fail] ([b])
  )