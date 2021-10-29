-- @Return int Name:A
-- @Return int Name:B
-- @Return int Name:C
-- @Return int Name:D
-- @Return int Name:E
CREATE PROCEDURE [dbo].[dbx_tests_parser_nestedifs]
AS
    DECLARE @true BIT = 1
    IF @true = 1
    BEGIN
        IF @true = 0
            SELECT 1.0 AS [action]
        ELSE
            SELECT 1.1 AS [action]

        SELECT 2 AS [action]
    END
    ELSE
    BEGIN
        SELECT 3 AS [action]

        IF @true = 0
            SELECT 4.0 AS [action]
        ELSE
            SELECT 4.1 AS [action]
    END

    
    IF @true = 1
        SELECT 1
    ELSE IF @true = 2
        SELECT 2
    ELSE
        SELECT 3

    
    IF @true = 1
        SELECT 1
    ELSE IF @true = 2
    BEGIN
        ;THROW 50000, N'Error', 1
    END
    ELSE
        SELECT 2

    IF @true = 1
    BEGIN
        IF @true = 2
        BEGIN
            ;THROW 50000, N'Error', 1
        END
        ELSE
        BEGIN
            ;THROW 50000, N'Error', 1
        END
    END
    ELSE
    BEGIN
        SELECT 1
    END