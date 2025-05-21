-- @Name IntParameterTwoSet
CREATE TYPE [dbo].[dbx_codeanalysis_udt_inttwo] AS TABLE
(
    [id1] INT NOT NULL
  , [id2] INT NOT NULL
  , PRIMARY KEY([id1], [id2])
)