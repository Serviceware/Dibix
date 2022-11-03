-- @Name AssertAuthorized
CREATE PROCEDURE [dbo].[dbx_tests_authorization] @right TINYINT
AS
	IF @right = 1
	BEGIN
		;THROW 403001, N'Forbidden', 1
	END