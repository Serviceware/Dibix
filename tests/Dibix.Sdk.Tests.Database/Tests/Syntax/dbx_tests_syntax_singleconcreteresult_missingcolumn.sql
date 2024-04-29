-- @Return ClrTypes:GenericContract Mode:Single
CREATE PROCEDURE [dbo].[dbx_tests_syntax_singleconcreteresult_missingcolumn]
AS
    SELECT [id]           = 1
         , [name]         = NULL
         , [parentid]     = NULL
         , [role]         = NULL
         , [creationtime] = NULL