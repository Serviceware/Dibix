CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_022]
AS
  SELECT TOP 1 [x].[a]
  FROM (VALUES (1)) AS [x]([a])

  SELECT [x].[a]
  FROM (VALUES (1)) AS [x]([a])
  ORDER BY [x].[a]
  OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY