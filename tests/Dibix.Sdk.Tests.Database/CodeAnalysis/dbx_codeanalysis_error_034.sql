CREATE TABLE [dbo].[dbx_codeanalysis_error_034_a]
(
	[x] TINYINT NOT NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_034_a] PRIMARY KEY ([x])
)
GO
CREATE TABLE [dbo].[dbx_codeanalysis_error_034_b]
(
	[x] INT NOT NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_034_b] PRIMARY KEY ([x])
)
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_034]
AS
	SELECT [a].[x]
	FROM [dbo].[dbx_codeanalysis_error_034_a] AS [a]
	INNER JOIN [dbo].[dbx_codeanalysis_error_034_b] AS [b] ON [a].[x] > [b].[x]-- AND 1 = 1 AND [a].[x] = [b].[x]