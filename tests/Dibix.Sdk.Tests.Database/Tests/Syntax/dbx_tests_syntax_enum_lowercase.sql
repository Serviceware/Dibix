-- Test new @enum pattern (lowercase) - should work after our changes
-- Both @Enum and @enum patterns are now supported for enum markup
-- This demonstrates the new case-insensitive enum pattern support
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