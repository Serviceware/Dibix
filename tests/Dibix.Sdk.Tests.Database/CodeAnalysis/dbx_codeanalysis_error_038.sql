CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_038] @id INT, @name NVARCHAR(128)
AS
	DECLARE @var INT
	PRINT @name
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_038_x] @id INT, @name NVARCHAR(128)
AS
	PRINT @id + @name
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_038_y] @id INT, @name NVARCHAR(128)
AS
BEGIN
	DECLARE @x TABLE([xml] XML)
	UPDATE @x SET [xml].modify(N'insert attribute name {sql:variable("@name")} into (/root/object[@id=sql:variable("@id")])[1]')
END