﻿-- @Namespace Grid
-- @Name GetGrid
-- @Return ClrTypes:Extension.MultiMapContract;GenericContract SplitOn:id Mode:SingleOrDefault
-- @Return ClrTypes:Direction Name:Directions
-- @MergeGridResult
CREATE PROCEDURE [dbo].[dbx_tests_syntax_gridresult_merge]
AS
	SELECT [x] = N'527B8008-AE6E-421F-91B2-5A0583070BCD', [id] = 1
	UNION ALL
	SELECT [x] = N'527B8008-AE6E-421F-91B2-5A0583070BCD', [id] = 2

	SELECT 1