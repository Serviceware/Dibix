CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_024]
AS
  CREATE TABLE [dbx_codeanalysis_error_024_fail] -- Missing business key => Fail
  (
      [a] INT IDENTITY
    , [b] NVARCHAR(128)
    , CONSTRAINT [PK_dbx_codeanalysis_error_024_table] PRIMARY KEY ([a])
  )

  CREATE TABLE [dbx_codeanalysis_error_024_success1] -- No identity column, probably INT business key => OK
  (
      [a] INT
    , [b] NVARCHAR(128)
    , CONSTRAINT [PK_dbx_codeanalysis_error_024_success1] PRIMARY KEY ([a])
  )

  CREATE TABLE [dbx_codeanalysis_error_024_success2] -- Business key is defined => OK
  (
      [a] INT IDENTITY
    , [b] NVARCHAR(128)
    , CONSTRAINT [PK_dbx_codeanalysis_error_024_success2] PRIMARY KEY ([a])
    , CONSTRAINT [UQ_dbx_codeanalysis_error_024_success2_b] UNIQUE ([b])
  )