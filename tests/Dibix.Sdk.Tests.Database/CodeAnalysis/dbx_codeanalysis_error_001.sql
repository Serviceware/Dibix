CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_001]
AS
	set nocount on

	DECLARE @v sys.sysname
	DECLARE @w sysname
	DECLARE @x nvarchar(max)
	DECLARE @y decimal(5,2)
	DECLARE @z XML

	DECLARE @a1 NVARCHAR(MAX) = @z.VALUE()
	DECLARE @a2 NVARCHAR(MAX) = @z.value()
	DECLARE @a3 INT = @z.NoDeS()
	DECLARE @a4 INT = @z.nodes()
	DECLARE @a5 DATETIME = cast(0 AS DATETIME)

	SeLeCT @x = count(id)
	FROM dbo.dbx_table

	DECLARE @b dbo.dbx_codeanalysis_udt_generic

	DECLARE @xml XML
	SELECT @xml.value(NULL, NULL)
	SELECT 1
	FROM @xml.nodes(NULL) AS [x]([a])
	SELECT @xml.query(NULL)

	SELECT row_number() over(partition by id order by id)
	FROM dbo.dbx_table