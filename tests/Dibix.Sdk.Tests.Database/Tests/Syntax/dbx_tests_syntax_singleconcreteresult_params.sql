-- @Name SingleConrecteResultWithParams
-- @Return ClrTypes:GenericContract Mode:Single
-- @Async
CREATE PROCEDURE [dbo].[dbx_tests_syntax_singleconcreteresult_params] @id INT, @name NVARCHAR(255)
AS
    SELECT [id]           = @id
         , [name]         = @name
         , [parentid]     = NULL
         , [role]         = NULL
         , [creationtime] = NULL
         , [imageurl]     = NULL