-- @Name AssertAuthorized
CREATE PROCEDURE [dbo].[dbx_tests_authorization] @right TINYINT
AS
BEGIN
	IF @right = 1
	BEGIN
		DECLARE @errormessage NVARCHAR(MAX) = N'Forbidden'
		SET @errormessage = CONCAT(@errormessage, N'. Expected the right: ', @right, N'!')
		;THROW 403001, @errormessage, 1
	END
END