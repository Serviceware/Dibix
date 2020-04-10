CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_038] @id INT, @name NVARCHAR(128)
AS
	DECLARE @var INT
	PRINT @name
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_038_x] @id INT, @name NVARCHAR(128)
AS
	PRINT @id + @name