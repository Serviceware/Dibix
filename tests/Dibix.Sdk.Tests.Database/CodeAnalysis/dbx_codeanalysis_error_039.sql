CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_039_x] @id INT, @name NVARCHAR(128), @age TINYINT
AS
	;
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_039]
AS
	DECLARE @x INT
	DECLARE @name_ NVARCHAR(128)
	DECLARE @age TINYINT

	EXEC [dbo].[dbx_codeanalysis_error_039_x]
	EXEC [dbo].[dbx_codeanalysis_error_039_x] @id = @x, @name = @name_, @age = @age
	EXEC [dbo].[dbx_codeanalysis_error_039_x] @x, @name_, @age = @age

	DECLARE @procedurename NVARCHAR(50) = N'[dbo].[dbx_codeanalysis_error_039_x]'
	EXECUTE @procedurename
	EXECUTE @procedurename @id = @x, @name = @name_, @age = @age
	EXECUTE @procedurename @x, @name_, @age = @age