CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_023]
AS
  CREATE TABLE [dbo].[dbx_codeanalysis_error_023_fail1]
  (
      [a] INT
    , [b] NVARCHAR(128)
    , CONSTRAINT [PK_dbx_codeanalysis_error_023_fail1] PRIMARY KEY ([a], [b])
    , CONSTRAINT [UQ_dbx_codeanalysis_error_023_fail1_b] UNIQUE ([b])
  )

  CREATE TABLE [dbo].[dbx_codeanalysis_error_023_fail2]
  (
      [b] NVARCHAR(128) PRIMARY KEY
  )

  CREATE TABLE [dbo].[dbx_codeanalysis_error_023_success] -- PK = FK
  (
      [b] NVARCHAR(128)
    , CONSTRAINT [PK_dbx_codeanalysis_error_023_success] PRIMARY KEY ([b])
    , CONSTRAINT [FK_dbx_codeanalysis_error_023_success_b] FOREIGN KEY ([b]) REFERENCES [dbo].[dbx_codeanalysis_error_023_fail1] ([b])
  )