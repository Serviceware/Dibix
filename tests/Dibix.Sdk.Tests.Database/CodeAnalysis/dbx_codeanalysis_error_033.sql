CREATE TABLE [dbo].[dbx_codeanalysis_error_033_fail1]
(
	[a] TINYINT NOT NULL
  , [b] TINYINT NOT NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_033_fail1] PRIMARY KEY ([a])
  , CONSTRAINT [UQ_dbx_codeanalysis_error_033_fail1_a] UNIQUE ([a]) -- Duplicate index with PK
  , INDEX [IX_dbx_codeanalysis_error_033_fail1_a] ([a]) -- Duplicate index with PK
)
GO
CREATE NONCLUSTERED INDEX [IX_dbx_codeanalysis_error_033_fail1_b1] ON [dbo].[dbx_codeanalysis_error_033_fail1] ([b]) WHERE ([a] > 1) -- Duplicate indexes
GO
CREATE NONCLUSTERED INDEX [IX_dbx_codeanalysis_error_033_fail1_b2] ON [dbo].[dbx_codeanalysis_error_033_fail1] ([b]) WHERE ([a]>1)
GO

CREATE TABLE [dbo].[dbx_codeanalysis_error_033_fail2]
(
	[a] TINYINT NOT NULL
  , [b] TINYINT NOT NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_033_fail2] PRIMARY KEY ([a])
)
GO
CREATE NONCLUSTERED INDEX [IX_dbx_codeanalysis_error_033_fail2_a] ON [dbo].[dbx_codeanalysis_error_033_fail2] ([a]) INCLUDE ([b]) -- Different includes
GO

CREATE TABLE [dbo].[dbx_codeanalysis_error_033_success]
(
	[a] TINYINT NOT NULL
  , [b] TINYINT NOT NULL
  , [c] TINYINT NOT NULL
  , [d] TINYINT NOT NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_033_success] PRIMARY KEY ([a])
  , CONSTRAINT [UQ_dbx_codeanalysis_error_033_success_b] UNIQUE ([b])
  , INDEX [IX_dbx_codeanalysis_error_033_success_c] ([c])
)
GO
CREATE NONCLUSTERED INDEX [IX_dbx_codeanalysis_error_033_success_d1] ON [dbo].[dbx_codeanalysis_error_033_success] ([d])
GO
CREATE NONCLUSTERED INDEX [IX_dbx_codeanalysis_error_033_success_d2] ON [dbo].[dbx_codeanalysis_error_033_success] ([d]) WHERE ([a] > 0)