﻿-- @Name SingleConrecteResultWithArrayParam
-- @Return ClrTypes:GenericContract Mode:Single
CREATE PROCEDURE [dbo].[dbx_tests_syntax_singleconcreteresult_params_array] @ids [dbo].[dbx_codeanalysis_udt_int] READONLY
AS
    SELECT [id]           = 1
         , [name]         = NULL
         , [parentid]     = NULL
         , [role]         = NULL
         , [creationtime] = NULL
         , [imageurl]     = NULL
         , [thedate]      = NULL
         , [thetime]      = NULL
    FROM @ids