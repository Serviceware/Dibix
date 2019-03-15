CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_024]
AS
  CREATE TABLE [dbo].[dbx_codeanalysis_error_024_fail1] -- Missing business key => fail
  (
      [a] INT IDENTITY
    , [b] NVARCHAR(128)
    , CONSTRAINT [PK_dbx_codeanalysis_error_024_fail1] PRIMARY KEY ([a])
  )

  CREATE TABLE [dbo].[dbx_codeanalysis_error_024_fail2] -- Different syntax - Missing business key => fail
  (
      [a] INT IDENTITY
    , [b] NVARCHAR(128)
    , [c] NVARCHAR(128) -- < note the missing comma here
      CONSTRAINT [PK_dbx_codeanalysis_error_024_fail2] PRIMARY KEY ([a])
  )

  CREATE TABLE [dbo].[dbx_codeanalysis_error_024_success1] -- No identity column, probably INT business key => OK
  (
      [a] INT
    , [b] NVARCHAR(128)
    , CONSTRAINT [PK_dbx_codeanalysis_error_024_success1] PRIMARY KEY ([a])
  )

  CREATE TABLE [dbo].[dbx_codeanalysis_error_024_success2] -- Business key is defined => OK
  (
      [a] INT IDENTITY
    , [b] NVARCHAR(128)
    , CONSTRAINT [PK_dbx_codeanalysis_error_024_success2] PRIMARY KEY ([a])
    , CONSTRAINT [UQ_dbx_codeanalysis_error_024_success2_b] UNIQUE ([b])
  )

  CREATE TABLE [dbo].[dbx_codeanalysis_error_024_success3] -- Different syntax - Business key is defined => OK
  (
      [a] INT IDENTITY
    , [b] NVARCHAR(128)
    , [c] NVARCHAR(128)
      CONSTRAINT [PK_dbx_codeanalysis_error_024_success3] PRIMARY KEY ([a])
    , CONSTRAINT [UQ_dbx_codeanalysis_error_024_success3_b] UNIQUE ([b])
  )