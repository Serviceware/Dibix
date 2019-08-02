CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_029]
AS
  CREATE TABLE [dbo].[dbx_codeanalysis_error_029_table_success]
  (
    [id]   INT           NOT NULL CONSTRAINT [PK_dbx_codeanalysis_error_029_table_success] PRIMARY KEY
  , [name] NVARCHAR(128) NOT NULL
  )

  CREATE TABLE [dbo].[dbx_codeanalysis_error_029_table_fail]
  (
    [id]   INT           NOT NULL
  , [name] NVARCHAR(128) NOT NULL
    CONSTRAINT [PK_dbx_codeanalysis_error_029_table_fail] PRIMARY KEY ([id])
  )