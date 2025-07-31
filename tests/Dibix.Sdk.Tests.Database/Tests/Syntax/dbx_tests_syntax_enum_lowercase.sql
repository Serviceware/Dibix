-- Test new @enum pattern (should work after our changes)
-- @enum TestFeatureLowercase
-- @Namespace TestFeatures
CREATE TABLE [dbo].[test_enum_lowercase]
(
    [id] TINYINT NOT NULL
  , CONSTRAINT [PK_test_enum_lowercase] PRIMARY KEY ([id])
  , CONSTRAINT [CK_test_enum_lowercase_x] CHECK ([id] IN (201 -- Feature1
                                                         , 202 -- Feature2
                                                         , 203 -- Feature3
    ))
)