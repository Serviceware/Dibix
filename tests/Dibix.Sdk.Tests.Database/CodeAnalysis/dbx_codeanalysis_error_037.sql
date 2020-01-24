CREATE FUNCTION [dbo].[dbx_codeanalysis_error_037_scalar]()
RETURNS INT
BEGIN
	RETURN 1
END
GO

CREATE FUNCTION [dbo].[dbx_codeanalysis_error_037_tvf_inline]()
RETURNS TABLE
RETURN
	SELECT [x] = 1
GO

CREATE FUNCTION [dbo].[dbx_codeanalysis_error_037_tvf_noninline]()
RETURNS @x TABLE ([x] INT)
BEGIN
	RETURN
END
GO

CREATE TABLE [dbo].[dbx_codeanalysis_error_037_table]
(
	[id] INT NOT NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_037_table] PRIMARY KEY ([id])
  , CONSTRAINT [CK_dbx_codeanalysis_error_037_table_x] CHECK ([dbo].[dbx_codeanalysis_error_037_scalar]() = 0) -- OK
)
GO

CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_037]
AS
BEGIN
	
	DECLARE @x INT = (SELECT [dbo].[dbx_codeanalysis_error_037_scalar]()) -- OK
	SELECT @x = [dbo].[dbx_codeanalysis_error_037_scalar]() -- OK
	SET @x = [dbo].[dbx_codeanalysis_error_037_scalar]() -- OK

	SELECT 1
	FROM (VALUES (1), (2)) AS [x]([i])
	WHERE (SELECT [dbo].[dbx_codeanalysis_error_037_scalar]()) = 1 -- Fail

	SELECT [x].[i], [dbo].[dbx_codeanalysis_error_037_scalar]() -- Fail
	FROM (VALUES (1), (2)) AS [x]([i])

END