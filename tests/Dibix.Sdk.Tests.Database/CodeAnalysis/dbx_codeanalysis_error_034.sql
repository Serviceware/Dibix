CREATE TABLE [dbo].[dbx_codeanalysis_error_034]
(
    [a] TINYINT            NOT NULL
  , [b] TINYINT            NULL
  , [c] AS ([b]) PERSISTED
  , CONSTRAINT [PK_dbx_codeanalysis_error_034] PRIMARY KEY ([a])
  , CONSTRAINT [CK_dbx_codeanalysis_error_034_fail] CHECK ([a] > 0
                                                       AND [b] > 0
                                                       AND IIF([b] > 0, 1, 0) = 1)
  , CONSTRAINT [CK_dbx_codeanalysis_error_034_success1] CHECK (ISNULL([b], 0) > 0)
  , CONSTRAINT [CK_dbx_codeanalysis_error_034_success2] CHECK (IIF([b] IS NOT NULL AND [b] > 0, 1, 0) = 1)
  , CONSTRAINT [CK_dbx_codeanalysis_error_034_success3] CHECK ([b] IS NULL OR [b] > 0)
  , CONSTRAINT [CK_dbx_codeanalysis_error_034_success4] CHECK ([c] > 0)
)