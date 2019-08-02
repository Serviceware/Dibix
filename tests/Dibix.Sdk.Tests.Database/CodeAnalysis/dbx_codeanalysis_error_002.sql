CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_002]
AS
	DECLARE @t TABLE(x INT)

	SELECT id
	FROM @t

	SELECT id
	FROM dbx_table

	;WITH x1 AS (SELECT 1 AS a), y1 AS (SELECT 1 AS b)
	SELECT a
	FROM x1 AS x1x
	INNER JOIN y1 AS y1x ON x1x.a = y1x.b

	SELECT a
	FROM dbx_table AS x2
	INNER JOIN @t AS y2 ON x2.a = y2.b

	INSERT INTO @t (x) VALUES (1)

	INSERT INTO dbx_table (id) VALUES (1)

	INSERT INTO dbx_table (id) SELECT id FROM dbx_table

	UPDATE x3 SET id = 1 FROM dbx_table AS x3

	UPDATE dbx_table SET id = 1 WHERE id = 1

	UPDATE @t SET id = 1

	UPDATE x3 SET id = 1 FROM dbx_table AS x3 LEFT JOIN @t AS y3 ON x3.id = y3.x
	
	DELETE x4
	FROM dbx_table AS x4
	WHERE x4.id = 1

	DELETE 
	FROM dbx_table
	WHERE id = 1

	DELETE 
	FROM @t
	WHERE ISNULL(x, 0) = 1
	
	DELETE x5
	FROM (VALUES (1)) AS x5(x)
	WHERE x5.x = 1

	DECLARE @true BIT = 1
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
	ON (@true = 0)
	WHEN NOT MATCHED BY SOURCE THEN
		DELETE
	;

	MERGE @t AS target
	USING @t AS source
	ON (@true = 0)
	WHEN NOT MATCHED BY SOURCE THEN
		DELETE
	;

	CREATE TABLE dbxx 
	(
		id INT NOT NULL CONSTRAINT [PK_dbxx] PRIMARY KEY 
	)