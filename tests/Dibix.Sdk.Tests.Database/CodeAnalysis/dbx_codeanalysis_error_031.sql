CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_031]
AS
  SELECT 1
  WHERE 1 = 1
  
  DECLARE @a INT
  DECLARE @b INT
  SELECT 1 
  WHERE @a = @a AND @b = @b

  SELECT 1
  WHERE 1 = 2

  SELECT 1
  WHERE 1 > 2

  SELECT 1
  WHERE 1 <> 2

  SELECT 1
  WHERE 1 < 2

  SELECT 1
  WHERE 1 IS NOT NULL

  SELECT 1
  WHERE @a = @b