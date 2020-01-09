CREATE TABLE [dbo].[dbx_codeanalysis_error_024_fail1] -- Missing business key => fail
(
    [a] INT NOT NULL IDENTITY
  , [b] NVARCHAR(128) NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_024_fail1] PRIMARY KEY ([a])
)
GO
CREATE TABLE [dbo].[dbx_codeanalysis_error_024_fail2] -- Different syntax - Missing business key => fail
(
    [a] INT NOT NULL IDENTITY
  , [b] NVARCHAR(128) NULL
  , [c] NVARCHAR(128) NULL -- < note the missing comma here
    CONSTRAINT [PK_dbx_codeanalysis_error_024_fail2] PRIMARY KEY ([a])
)
GO
CREATE TABLE [dbo].[dbx_codeanalysis_error_024_fail3] -- Business key is defined on PK => fail
(
    [a] INT NOT NULL IDENTITY
  , [b] NVARCHAR(128) NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_024_fail3] PRIMARY KEY ([a])
  , CONSTRAINT [UQ_dbx_codeanalysis_error_024_fail3_b] UNIQUE ([a])
)
GO
CREATE TABLE [dbo].[dbx_codeanalysis_error_024_fail4] -- Business key can be made as PK to replace the surrogate key and UQ => fail
(
    [a] INT NOT NULL IDENTITY
  , [b] INT NOT NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_024_fail4] PRIMARY KEY ([a])
  , CONSTRAINT [UQ_dbx_codeanalysis_error_024_fail4_b] UNIQUE ([b])
)
GO
CREATE TABLE [dbo].[dbx_codeanalysis_error_024_fail5] -- Identity column that is not part of a primary key => fail
(
    [a] INT NOT NULL
  , [b] INT NOT NULL IDENTITY
  , CONSTRAINT [PK_dbx_codeanalysis_error_024_fail5] PRIMARY KEY ([a])
)
GO
CREATE TABLE [dbo].[dbx_codeanalysis_error_024_success1] -- No identity column, probably INT business key => OK
(
    [a] INT NOT NULL
  , [b] NVARCHAR(128) NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_024_success1] PRIMARY KEY ([a])
)
GO
CREATE TABLE [dbo].[dbx_codeanalysis_error_024_success2] -- Business key is defined => OK
(
    [a] INT NOT NULL IDENTITY
  , [b] NVARCHAR(128) NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_024_success2] PRIMARY KEY ([a])
  , CONSTRAINT [UQ_dbx_codeanalysis_error_024_success2_b] UNIQUE ([b])
)
GO
CREATE TABLE [dbo].[dbx_codeanalysis_error_024_success3] -- Different syntax - Business key is defined => OK
(
    [a] INT NOT NULL IDENTITY
  , [b] NVARCHAR(128) NULL
  , [c] NVARCHAR(128) NULL
    CONSTRAINT [PK_dbx_codeanalysis_error_024_success3] PRIMARY KEY ([a])
  , CONSTRAINT [UQ_dbx_codeanalysis_error_024_success3_b] UNIQUE ([b])
)