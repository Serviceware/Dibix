-- @Enum
-- @Namespace Features
CREATE TABLE [dbo].[dbx_tests_syntax_enum3]
(
    [id]           SMALLINT     NOT NULL
  , [featurename]  NVARCHAR(50) NOT NULL
  , [categoryname] NVARCHAR(50) NOT NULL
  , CONSTRAINT [PK_dbx_tests_syntax_enum3] PRIMARY KEY ([id])
    -- @Enum Feature
  , CONSTRAINT [CK_dbx_tests_syntax_enum3_x] CHECK (([id]=(103)) -- Feature3
                                                 OR  [id]=(101)  -- Feature1
                                                 OR ([id]=(102)) -- Feature2
                                                 OR ([id]=(104) AND [featurename] = N'Feature4')
    )
    -- @Enum FeatureCategory
  , CONSTRAINT [CK_dbx_tests_syntax_enum3_y] CHECK (([id]=(103)) -- FeatureCategory3
                                                 OR  [id]=(101)  -- FeatureCategory1
                                                 OR ([id]=(102)) -- FeatureCategory2
                                                 OR ([id]=(104) AND [categoryname] = N'FeatureCategory4')
    )
)