CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_016]
AS
    CREATE TABLE [dbo].[dbx_codeanalysis_error_016_table]
    (
        [id] INT       NOT NULL
      , [a]  TEXT      NOT NULL
      , [b]  NTEXT     NOT NULL
      , [c]  IMAGE     NOT NULL
      , [d]  DATETIME2 NOT NULL
      , CONSTRAINT [PK_dbx_codeanalysis_error_016_table] PRIMARY KEY ([id])
    )