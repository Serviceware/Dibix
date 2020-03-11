-- @Name GenericParameterSet
CREATE TYPE [dbo].[dbx_codeanalysis_udt_generic] AS TABLE
(
	[id]   INT          NOT NULL PRIMARY KEY
  , [name] NVARCHAR(50) NULL 
)
