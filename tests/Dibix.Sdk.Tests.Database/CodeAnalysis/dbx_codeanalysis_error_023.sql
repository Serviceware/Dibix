CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_023]
AS
  CREATE TABLE [dbo].[dbx_codeanalysis_error_023_fail1] -- Part of the PK has an invalid type -> fail
  (
      [a] INT NOT NULL
    , [b] NVARCHAR(128)
    , CONSTRAINT [PK_dbx_codeanalysis_error_023_fail1] PRIMARY KEY ([a], [b])
    , CONSTRAINT [UQ_dbx_codeanalysis_error_023_fail1_b] UNIQUE ([b])
  )

  CREATE TABLE [dbo].[dbx_codeanalysis_error_023_fail2] -- Same as 'fail1' but different PK syntax -> fail
  (
      [b] NVARCHAR(128) NOT NULL CONSTRAINT [PK_dbx_codeanalysis_error_023_fail2] PRIMARY KEY
  )

  CREATE TABLE [dbo].[dbx_codeanalysis_error_023_fail3] -- [b] is part of a FK, but [c] is not -> fail
  (
      [b] NVARCHAR(128) NOT NULL
    , [c] NVARCHAR(128) NOT NULL
    , CONSTRAINT [PK_dbx_codeanalysis_error_023_fail3] PRIMARY KEY ([b], [c])
    , CONSTRAINT [FK_dbx_codeanalysis_error_023_fail3_b] FOREIGN KEY ([b]) REFERENCES [dbo].[dbx_codeanalysis_error_023_fail1] ([b])
  )

  CREATE TABLE [dbo].[dbx_codeanalysis_error_023_success1] -- PK = FK -> success
  (
      [b] NVARCHAR(128) NOT NULL
    , CONSTRAINT [PK_dbx_codeanalysis_error_023_success1] PRIMARY KEY ([b])
    , CONSTRAINT [FK_dbx_codeanalysis_error_023_success1_b] FOREIGN KEY ([b]) REFERENCES [dbo].[dbx_codeanalysis_error_023_fail1] ([b])
  )

  CREATE TABLE [dbo].[dbx_codeanalysis_error_023_success2] -- PK part of FK -> success
  (
      [b] NVARCHAR(128) NOT NULL
    , [c] TINYINT       NOT NULL
    , CONSTRAINT [PK_dbx_codeanalysis_error_023_success2] PRIMARY KEY ([b], [c])
    , CONSTRAINT [FK_dbx_codeanalysis_error_023_success2_b] FOREIGN KEY ([b]) REFERENCES [dbo].[dbx_codeanalysis_error_023_fail1] ([b])
  )