CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_030]
AS
  SELECT HASHBYTES(N'SHA1', N'') -- Fail
  SELECT HASHBYTES(N'SHA2_512', N'') -- Success