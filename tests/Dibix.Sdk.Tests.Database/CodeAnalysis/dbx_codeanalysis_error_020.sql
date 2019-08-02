CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_020]
AS
  DECLARE @x DATETIME = CAST(N'1990-01-01' AS DATETIME)
  
  CREATE TABLE [dbo].[dbx_codeanalysis_error_020_table]
  (
      [id] INT NOT NULL
    , [value] DATETIME CONSTRAINT [DF_dbx_codeanalysis_error_020_table_value] DEFAULT ((N'1990-01-01'))
	, CONSTRAINT [PK_dbx_codeanalysis_error_020_table] PRIMARY KEY ([id])
  )