CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_017]
AS
	CREATE TABLE [dbo].[dbx_codeanalysis_error_017_success]
	(
		[id] INT CONSTRAINT [DF_dbx_codeanalysis_error_017_success_id] DEFAULT (1)
	  , CONSTRAINT [PK_dbx_codeanalysis_error_017_success] PRIMARY KEY ([id])
	  , CONSTRAINT [FK_dbx_codeanalysis_error_017_success_id] FOREIGN KEY ([id]) REFERENCES [dbo].[dbx_table] ([id])
	  , CONSTRAINT [CK_dbx_codeanalysis_error_017_success_abc] CHECK ([id] > 0)
	  , CONSTRAINT [UQ_dbx_codeanalysis_error_017_success_id] UNIQUE ([id])
	)

	CREATE TABLE [dbo].[dbx_codeanalysis_error_017_fail]
	(
		[Id] INT CONSTRAINT [DF_dbx_codeanalysis_error_017_fail_idx] DEFAULT (1)
	  ,	[column_x] INT
	  , CONSTRAINT [PK_dbx_codeanalysis_error_017_failx] PRIMARY KEY ([id])
	--, CONSTRAINT [FK_dbx_codeanalysis_error_017_fail_idx] FOREIGN KEY ([id]) REFERENCES [dbo].[dbx_table] ([id])
	  , CONSTRAINT [FK_dbx_codeanalysis_error_017_failx_id] FOREIGN KEY ([id]) REFERENCES [dbo].[dbx_table] ([id])
	  , CONSTRAINT [CK_dbx_codeanalysis_error_017_failx_abc] CHECK ([id] > 0)
	--, CONSTRAINT [UQ_dbx_codeanalysis_error_017_fail_idx] UNIQUE ([id])
	  , CONSTRAINT [UQ_dbx_codeanalysis_error_017_failx_id] UNIQUE ([id])
	)

	CREATE NONCLUSTERED INDEX [IX_dbx_codeanalysis_error_017_success_success] ON [dbo].[dbx_codeanalysis_error_017_success] ([id])
	CREATE NONCLUSTERED INDEX [IX_dbx_codeanalysis_error_017_successx_fail] ON [dbo].[dbx_codeanalysis_error_017_success] ([id])

	CREATE UNIQUE NONCLUSTERED INDEX [UQ_dbx_codeanalysis_error_017_success_success] ON [dbo].[dbx_codeanalysis_error_017_success] ([id])
	CREATE UNIQUE NONCLUSTERED INDEX [UQ_dbx_codeanalysis_error_017_successx_fail] ON [dbo].[dbx_codeanalysis_error_017_success] ([id])