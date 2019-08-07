CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_017]
AS
	CREATE TABLE [dbo].[dbx_codeanalysis_error_017_success]
	(
		[a] INT NOT NULL CONSTRAINT [DF_dbx_codeanalysis_error_017_success_a] DEFAULT (1)
      , [b] INT NULL
      , [c] INT NULL
      , [d] INT NULL
      , [e] INT NULL
	  , CONSTRAINT [PK_dbx_codeanalysis_error_017_success] PRIMARY KEY ([a])
	  , CONSTRAINT [FK_dbx_codeanalysis_error_017_success_a] FOREIGN KEY ([a]) REFERENCES [dbo].[dbx_table] ([id])
	  , CONSTRAINT [CK_dbx_codeanalysis_error_017_success_a] CHECK ([a] > 0)
	  , CONSTRAINT [UQ_dbx_codeanalysis_error_017_success_b] UNIQUE ([b])
	  , INDEX [IX_dbx_codeanalysis_error_017_success_c] ([c])
	)
	CREATE NONCLUSTERED INDEX [IX_dbx_codeanalysis_error_017_success_d] ON [dbo].[dbx_codeanalysis_error_017_success] ([d])
	CREATE UNIQUE NONCLUSTERED INDEX [UQ_dbx_codeanalysis_error_017_success_e] ON [dbo].[dbx_codeanalysis_error_017_success] ([e]) INCLUDE ([a])


	CREATE TABLE [dbo].[dbx_codeanalysis_error_017_fail]
	(
		[A] INT NOT NULL CONSTRAINT [DF_dbx_codeanalysis_error_017_fail_idx] DEFAULT (1)
	  ,	[b] INT NULL
      , [c] INT NULL
      , [d] INT NULL
      , [e] INT NULL
	  , CONSTRAINT [PK_dbx_codeanalysis_error_017_failx] PRIMARY KEY ([A])
	  , CONSTRAINT [FK_dbx_codeanalysis_error_017_failx_a] FOREIGN KEY ([A]) REFERENCES [dbo].[dbx_table] ([id])
	  , CONSTRAINT [CK_dbx_codeanalysis_error_017_failx_a] CHECK ([A] > 0)
	  , CONSTRAINT [UQ_dbx_codeanalysis_error_017_failx_b] UNIQUE ([b])
	  , INDEX [IX_dbx_codeanalysis_error_017_failx_c] ([c])
	)
	CREATE NONCLUSTERED INDEX [IX_dbx_codeanalysis_error_017_failx_d] ON [dbo].[dbx_codeanalysis_error_017_fail] ([d])
	CREATE UNIQUE NONCLUSTERED INDEX [UQ_dbx_codeanalysis_error_017_failx_e] ON [dbo].[dbx_codeanalysis_error_017_fail] ([e]) INCLUDE ([A])


	CREATE TYPE [dbo].[dbx_codeanalysis_error_017_fail_udt] AS TABLE ([id] INT NOT NULL PRIMARY KEY)
	CREATE TYPE [dbo].[dbx_codeanalysis_udt_error_017_success] AS TABLE ([id] INT NOT NULL PRIMARY KEY)