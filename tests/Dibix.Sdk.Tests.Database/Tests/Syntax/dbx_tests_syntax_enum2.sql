-- @Enum SecurityPolicy
-- @Namespace Policies
CREATE TABLE [dbo].[dbx_tests_syntax_enum2]
(
    [id] INT NOT NULL
        CONSTRAINT [PK_dbx_tests_syntax_enum2] PRIMARY KEY
        CONSTRAINT [CK_dbx_tests_syntax_enum2_x] CHECK ([id]=(103) -- Feature3
                                                     OR [id]=(101) -- Feature1
                                                     OR [id]=(102) -- Feature2
        )
)