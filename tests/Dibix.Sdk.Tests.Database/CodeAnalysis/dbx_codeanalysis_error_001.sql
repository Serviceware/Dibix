CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_001]
AS
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