﻿-- @Enum Feature
-- @Namespace Features
CREATE TABLE [dbo].[dbx_tests_syntax_enum1]
(
    [id] TINYINT NOT NULL
  , CONSTRAINT [PK_dbx_tests_syntax_enum1] PRIMARY KEY ([id])
  , CONSTRAINT [CK_dbx_tests_syntax_enum1_x] CHECK ([id] IN (0   -- Unknown
                                                           , 101 -- Feature1
                                                           , 102 -- Feature2
                                                           , 103 -- Feature3
    ))
)