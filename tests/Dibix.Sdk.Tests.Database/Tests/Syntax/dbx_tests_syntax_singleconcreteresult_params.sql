-- @Name SingleConrecteResultWithParams
-- @Return ClrTypes:GenericContract Mode:Single
CREATE PROCEDURE [dbo].[dbx_tests_syntax_singleconcreteresult_params] @ids [dbo].[dbx_codeanalysis_udt_int] READONLY
AS
    SELECT [id]           = 1
         , [name]         = NULL
         , [parentid]     = NULL
         , [role]         = NULL
         , [creationtime] = NULL
         , [imageurl]     = NULL
    FROM @ids