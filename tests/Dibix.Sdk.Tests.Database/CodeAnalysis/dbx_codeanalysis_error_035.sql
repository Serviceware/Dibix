CREATE TABLE [dbo].[dbx_codeanalysis_error_035_1]
(
    [a] TINYINT NOT NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_035_1] PRIMARY KEY ([a])
  , CONSTRAINT [CK_dbx_codeanalysis_error_035_1_fail1] CHECK ([a] > 0)
  , CONSTRAINT [CK_dbx_codeanalysis_error_035_1_fail2] CHECK ([a] > 0)
)
GO
CREATE TABLE [dbo].[dbx_codeanalysis_error_035_2]
(
    [a] TINYINT NOT NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_035_2] PRIMARY KEY ([a])
  , CONSTRAINT [CK_dbx_codeanalysis_error_035_2_success] CHECK ([a] > 0)
)