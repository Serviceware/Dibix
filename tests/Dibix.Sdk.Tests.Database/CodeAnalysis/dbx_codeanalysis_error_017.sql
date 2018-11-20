CREATE TABLE [dbo].[dbx_codeanalysis_error_017]
(
	[id] INT CONSTRAINT [DF_dbx_codeanalysis_error_017_id] DEFAULT (1)
  , CONSTRAINT [PK_dbx_codeanalysis_error_017] PRIMARY KEY ([id])
  , CONSTRAINT [FK_dbx_codeanalysis_error_017_id] FOREIGN KEY ([id]) REFERENCES [dbo].[dbx_table] ([id])
  , CONSTRAINT [CK_dbx_codeanalysis_error_017_abc] CHECK ([id] > 0)
  , CONSTRAINT [UQ_dbx_codeanalysis_error_017_id] UNIQUE ([id])
)
GO
CREATE TABLE [dbo].[dbx_codeanalysis_error_017_fail]
(
	[id] INT CONSTRAINT [DF_dbx_codeanalysis_error_017_fail_idx] DEFAULT (1)
  , CONSTRAINT [PK_dbx_codeanalysis_error_017_failx] PRIMARY KEY ([id])
--, CONSTRAINT [FK_dbx_codeanalysis_error_017_fail_idx] FOREIGN KEY ([id]) REFERENCES [dbo].[dbx_table] ([id])
  , CONSTRAINT [FK_dbx_codeanalysis_error_017_failx_id] FOREIGN KEY ([id]) REFERENCES [dbo].[dbx_table] ([id])
  , CONSTRAINT [CK_dbx_codeanalysis_error_017_failx_abc] CHECK ([id] > 0)
--, CONSTRAINT [UQ_dbx_codeanalysis_error_017_fail_idx] UNIQUE ([id])
  , CONSTRAINT [UQ_dbx_codeanalysis_error_017_failx_id] UNIQUE ([id])
)