CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_003_fail]
AS
	RETURN
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_003_success1]
AS
	RETURN 0
GO
CREATE FUNCTION [dbo].[dbx_codeanalysis_error_003_success2]()
RETURNS @rtnTable TABLE([a] INT) 
AS
BEGIN
	RETURN
END
GO
CREATE FUNCTION [dbo].[dbx_codeanalysis_error_003_success3]()
RETURNS TABLE
RETURN
	SELECT [x] = 1