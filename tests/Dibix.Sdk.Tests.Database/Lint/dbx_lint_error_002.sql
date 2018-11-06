CREATE PROCEDURE [dbo].[dbx_lint_error_002]
AS
	DECLARE @t TABLE(x INT)

	SELECT id
	FROM @t

	SELECT id
	FROM dbx_table

	;WITH x1 AS (SELECT 1 AS a), y1 AS (SELECT 1 AS b)
	SELECT a
	FROM x1
	INNER JOIN y1 ON x1.a = y1.b

	SELECT a
	FROM dbx_table AS x2
	INNER JOIN @t AS y2 ON x2.a = y2.b

	INSERT INTO @t VALUES (1)

	INSERT INTO dbx_table VALUES (1)

	INSERT INTO dbx_table SELECT id FROM dbx_table

	UPDATE x3 SET id = 1 FROM dbx_table AS x3

	UPDATE dbx_table SET id = 1

	UPDATE @t SET id = 1

	UPDATE x3 SET id = 1 FROM dbx_table AS x3 LEFT JOIN @t AS y3 ON x3.id = y3.x
	
	DELETE x4
	FROM dbx_table AS x4

	DELETE 
	FROM dbx_table

	DELETE 
	FROM @t
	
	DELETE x5
	FROM (VALUES (1)) AS x5

	;WITH x6 AS 
	(
		SELECT id
		FROM dbx_table
	), x7 AS
	(
		SELECT id
		FROM dbx_table
	)
	MERGE x6 AS target
	USING x7 AS source
	ON (1 = 0)
	WHEN NOT MATCHED BY SOURCE THEN
		DELETE
	;

	MERGE @t AS target
	USING @t AS source
	ON (1 = 0)
	WHEN NOT MATCHED BY SOURCE THEN
		DELETE
	;