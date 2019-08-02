CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_014]
AS
	CREATE TABLE [dbo].[dbx_codeanalysis_error_014_fail1]
	(
		[id] INT NOT NULL PRIMARY KEY DEFAULT 1
	)
	
	CREATE TABLE [dbo].[dbx_codeanalysis_error_014_success1]
	(
		[id] INT NOT NULL CONSTRAINT [PK_dbx_codeanalysis_error_014_success1] PRIMARY KEY CONSTRAINT [DF_dbx_codeanalysis_error_014_success1_id] DEFAULT 1
	)

	CREATE TABLE [dbo].[dbx_codeanalysis_error_014_fail2]
	(
		[id] INT NOT NULL
	  , PRIMARY KEY ([id])
	)

	CREATE TABLE [dbo].[dbx_codeanalysis_error_014_success2]
	(
		[id] INT NOT NULL
	  , CONSTRAINT [PK_dbx_codeanalysis_error_014_success2] PRIMARY KEY ([id])
	)