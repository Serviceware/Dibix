-- @Name FileResult
-- @FileResult
CREATE PROCEDURE [dbo].[dbx_tests_syntax_fileresult] @id INT
AS
BEGIN
    DECLARE @table TABLE([id] INT NOT NULL, [thumbnail] VARBINARY(MAX) NOT NULL, PRIMARY KEY ([id]))

    --DECLARE @id INT = 1
    DECLARE @extension NCHAR(3) = N'png'
    DECLARE @fallbackimageid INT = 666
    DECLARE @fallbackimagedata VARBINARY(MAX) = 0x1

    -- TODO: Improve column analyzer to avoid these CASTs
    IF @id = @fallbackimageid
    BEGIN
        SELECT [type] = CAST(@extension AS NVARCHAR(3))
             , [data] = CAST(@fallbackimagedata AS VARBINARY(MAX))
    END
    ELSE
    BEGIN
        SELECT [type] = @extension
             , [data] = [thumbnail]
        FROM @table
        WHERE [id] = @id
    END
END