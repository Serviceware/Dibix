CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_039_x] @id INT, @name NVARCHAR(128)
AS
	DECLARE @var INT
	PRINT @name
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_039]
AS
	DECLARE @x INT
	DECLARE @name NVARCHAR(128)

	EXEC [dbo].[dbx_codeanalysis_error_039_x]
	EXEC [dbo].[dbx_codeanalysis_error_039_x] @id = @x, @name = @name
	EXEC [dbo].[dbx_codeanalysis_error_039_x] @x, @name