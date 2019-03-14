﻿CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_028]
AS
  CREATE TABLE [dbo].[dbx_codeanalysis_error_028_table]
  (
    [id] INT NOT NULL CONSTRAINT [PK_dbx_codeanalysis_error_028_table] PRIMARY KEY
  , [x] DATETIME NOT NULL CONSTRAINT [DF_dbx_codeanalysis_error_028_table_x] DEFAULT (GETDATE())
  , [y] DATETIME NOT NULL CONSTRAINT [DF_dbx_codeanalysis_error_028_table_x] DEFAULT (N'1990-01-01')
  )