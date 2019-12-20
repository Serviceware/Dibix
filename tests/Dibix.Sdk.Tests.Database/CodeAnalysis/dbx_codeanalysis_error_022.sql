CREATE TABLE [dbo].[dbx_codeanalysis_error_022_table]
(
    [a] INT NOT NULL
  , [b] INT NOT NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_022_table] PRIMARY KEY ([a], [b])
)
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_022]
AS
  SELECT TOP 1 [x].[a]
  FROM (VALUES (1)) AS [x]([a])

  SELECT [x].[a]
  FROM (VALUES (1)) AS [x]([a])
  ORDER BY [x].[a]
  OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY

  SELECT TOP 1 1
  FROM [dbo].[dbx_codeanalysis_error_022_table]
  WHERE [a] = 1 AND [b] > 0 AND [b] = 2

  DECLARE @t TABLE([x] INT NOT NULL, [i] INT NOT NULL, PRIMARY KEY([i]))

  SELECT TOP 1 1
  FROM @t
  WHERE [x] = 1 AND [i] = 5