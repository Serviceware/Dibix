CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_001]
AS
	set nocount on

	DECLARE @v sys.sysname
	DECLARE @w sysname
	DECLARE @x nvarchar(max)
	DECLARE @y decimal(5,2)
	DECLARE @z bigint = row_number()
	DECLARE @a XML

	DECLARE @a1 NVARCHAR(MAX) = @a.VALUE()
	DECLARE @a2 NVARCHAR(MAX) = @a.value()
	DECLARE @a3 INT = @a.NoDeS()
	DECLARE @a4 INT = @a.nodes()

	SeLeCT @x = count(id)
	FROM dbo.dbx_table

	DECLARE @b dbo.dbx_udt

	DECLARE @xml XML
	SELECT @xml.value(NULL, NULL)
	SELECT 1
	FROM @xml.nodes(NULL) AS [x]([a])
	SELECT @xml.query(NULL)