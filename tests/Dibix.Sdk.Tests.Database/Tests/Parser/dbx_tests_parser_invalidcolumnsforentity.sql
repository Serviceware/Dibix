-- @Return ClrTypes:int32? Name:A
-- @Return ClrTypes:int32 Name:B
-- @Return ClrTypes:string Name:C
-- @Return ClrTypes:Dibix.Sdk.VisualStudio.Tests.Direction? Name:D
-- @Return ClrTypes:Dibix.Sdk.VisualStudio.Tests.SpecialEntity Name:E
CREATE PROCEDURE [dbo].[dbx_tests_parser_invalidcolumnsforentity]
AS
	SELECT COUNT(*) AS [column] FROM (VALUES(1)) AS x(a)

	;WITH [x] AS (
		SELECT 1 AS [i]
	)
	SELECT [i] AS [y]
	FROM [x]

	DECLARE @true BIT = 1
	IF @true = 1
	BEGIN
		SELECT 1 AS [action]
	END
	ELSE 
	/*
	IF @true = 1
	BEGIN
		SELECT 2AS [action]
	END
	ELSE IF @true = 1
	BEGIN
		SELECT 3 AS [action]
	END
	ELSE
	*/
		SELECT 4 AS [action]

	/*
	IF @true = 1
	BEGIN
		SELECT 1 AS [action]
	END

	IF @true = 1
	BEGIN
		SELECT 1 AS [a]
	END
	ELSE
		SELECT 4 AS [b]

	IF @true = 1
	BEGIN
		SELECT 1 AS [a], 2 AS [x]
	END
	ELSE
		SELECT 4 AS [b]

	IF @true = 1
	BEGIN
		SELECT 1 AS [x]
		SELECT 2 AS [x]
	END
	ELSE
		SELECT 3 AS [x]
	*/

	MERGE dbo.dbx_table AS target
	USING dbo.dbx_table AS source
	ON (@true = 0)
	WHEN NOT MATCHED BY SOURCE THEN
		DELETE
		OUTPUT $action AS [action]
	;

	SELECT 1 AS id, N'Cake' AS [name] /* [namex] */, 12 AS [age]
	UNION ALL
	SELECT 2 AS id, N'	Cookie' AS [name] /* [namex] */, 16 AS [age]

	/*
	SELECT [x].[i]
	FROM (VALUES (1)) AS [x]([i])
	UNION ALL
	SELECT [x].[i]
	FROM (
		SELECT 1 AS [i]
	) AS [x]
	*/