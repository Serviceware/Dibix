-- @Return ClrTypes:#MultiMapContract;#GenericContract SplitOn:a Mode:Single
CREATE PROCEDURE [dbo].[dbx_tests_syntax_singlemultimapresult]
AS
	SELECT [x] = N'527B8008-AE6E-421F-91B2-5A0583070BCD', [a] = 1
	UNION ALL
	SELECT [x] = N'527B8008-AE6E-421F-91B2-5A0583070BCD', [a] = 2