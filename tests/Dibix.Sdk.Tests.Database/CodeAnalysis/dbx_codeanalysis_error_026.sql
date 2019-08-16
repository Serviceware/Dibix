CREATE TABLE [dbo].[dbx_codeanalysis_error_026_table]
(
	[a] TINYINT NOT NULL CONSTRAINT [PK_dbx_codeanalysis_error_026_table] PRIMARY KEY
  , [b] TINYINT NOT NULL
)
GO
ALTER TABLE [dbo].[dbx_codeanalysis_error_026_table] ADD CONSTRAINT [UQ_dbx_codeanalysis_error_026_table_b] UNIQUE ([b])