CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_026]
AS
  CREATE TABLE [dbo].[dbx_codeanalysis_error_026_table]
  (
    [id] INT NOT NULL CONSTRAINT [PK_dbx_codeanalysis_error_026_table] PRIMARY KEY
  )

  ALTER TABLE [dbo].[dbx_codeanalysis_error_026_table] ADD CONSTRAINT [UQ_dbx_codeanalysis_error_026_table] UNIQUE ([id])