-- @Enum Feature
-- @Namespace Features
CREATE TABLE [dbo].[dbx_tests_syntax_enum2]
(
    [id]   SMALLINT     NOT NULL
  , [type] TINYINT      NOT NULL
  , [name] NVARCHAR(50) NOT NULL
  , CONSTRAINT [PK_dbx_tests_syntax_enum2] PRIMARY KEY ([id])
  , CONSTRAINT [CK_dbx_tests_syntax_enum2_x] CHECK ([type] = 1) -- 1:'Type1'
  , CONSTRAINT [CK_dbx_tests_syntax_enum2_y] CHECK (([id]=(103)) -- Feature3
                                                 OR  [id]=(101)  -- Feature1
                                                 OR ([id]=(102)) -- Feature2
                                                 OR ([id]=(104) AND [name] = N'Feature4')
    )
)