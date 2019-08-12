CREATE TABLE [dbo].[dbx_codeanalysis_error_032_fail1]
(
	[a] BIT            NOT NULL -- 1   byte
  , [b] TINYINT        NOT NULL -- 1   byte
  , [c] SMALLINT       NOT NULL -- 2   bytes
  , [d] INT            NOT NULL -- 4   bytes
  , [e] BIGINT         NOT NULL -- 8   bytes
  , [f] DECIMAL        NOT NULL -- 9   bytes   => Default: DECIMAL(18,0) 
  , [g] DECIMAL(9)     NOT NULL -- 5   bytes   Precision 1-9   => 5  storage bytes
  , [h] DECIMAL(9,5)   NOT NULL -- 5   bytes   Precision 1-9   => 5  storage bytes
  , [i] DECIMAL(10,5)  NOT NULL -- 9   bytes   Precision 10-19 => 9  storage bytes
  , [j] DECIMAL(20,10) NOT NULL -- 13  bytes   Precision 20-28 => 13 storage bytes
  , [k] DECIMAL(29,15) NOT NULL -- 17  bytes   Precision 29-38 => 17 storage bytes
  , [l] DATE           NOT NULL -- 4   bytes	  
  , [m] DATETIME       NOT NULL -- 8   bytes	  
  , [n] CHAR           NOT NULL -- 1   byte    Default: CHAR(1)
  , [o] CHAR(815)      NOT NULL -- 815 bytes	  
  , CONSTRAINT [PK_dbx_codeanalysis_error_032_fail1] PRIMARY KEY ([a], [b], [c], [d], [e], [f], [g], [h], [i], [j], [k], [l], [m], [n], [o])
)
GO
CREATE TABLE [dbo].[dbx_codeanalysis_error_032_fail2]
(
    [r]  VARCHAR          NOT NULL -- 1   byte    Default: VARCHAR(1)
  , [s]  VARCHAR(128)     NOT NULL -- 128 bytes	  
  , [t]  NVARCHAR         NOT NULL -- 2   bytes   Default: NVARCHAR(1)
  , [u]  NVARCHAR(128)    NOT NULL -- 256 bytes
  , [v]  SYSNAME          NOT NULL -- 256 bytes   VARCHAR(256)
  , [w]  UNIQUEIDENTIFIER NOT NULL -- 16  bytes	  
  , [x]  BINARY           NOT NULL -- 1   byte    Default: BINARY(1)
  , [y]  BINARY(2)        NOT NULL -- 2   bytes	  
  , [z]  VARBINARY        NOT NULL -- 1   byte    Default: VARBINARY(1)
  , [aa] VARBINARY(238)   NOT NULL -- 238 bytes
  , [ab] VARBINARY(1701)  NOT NULL
  , [ac] VARBINARY(1701)  NOT NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_032_fail2] PRIMARY KEY ([r], [s], [t], [u], [v], [w], [x], [y], [z], [aa])
  , CONSTRAINT [UQ_dbx_codeanalysis_error_032_fail2_ab] UNIQUE ([ab])
  , INDEX [IX_dbx_codeanalysis_error_032_fail2_ac] ([ac])
)
GO

CREATE TABLE [dbo].[dbx_codeanalysis_error_032_success1]
(
	[a] BIT            NOT NULL -- 1   byte
  , [b] TINYINT        NOT NULL -- 1   byte
  , [c] SMALLINT       NOT NULL -- 2   bytes
  , [d] INT            NOT NULL -- 4   bytes
  , [e] BIGINT         NOT NULL -- 8   bytes
  , [f] DECIMAL        NOT NULL -- 9   bytes   => Default: DECIMAL(18,0) 
  , [g] DECIMAL(9)     NOT NULL -- 5   bytes   Precision 1-9   => 5  storage bytes
  , [h] DECIMAL(9,5)   NOT NULL -- 5   bytes   Precision 1-9   => 5  storage bytes
  , [i] DECIMAL(10,5)  NOT NULL -- 9   bytes   Precision 10-19 => 9  storage bytes
  , [j] DECIMAL(20,10) NOT NULL -- 13  bytes   Precision 20-28 => 13 storage bytes
  , [k] DECIMAL(29,15) NOT NULL -- 17  bytes   Precision 29-38 => 17 storage bytes
  , [l] DATE           NOT NULL -- 4   bytes	  
  , [m] DATETIME       NOT NULL -- 8   bytes	  
  , [n] CHAR           NOT NULL -- 1   byte    Default: CHAR(1)
  , [o] CHAR(814)      NOT NULL -- 814 bytes	  
  , CONSTRAINT [PK_dbx_codeanalysis_error_032_success1] PRIMARY KEY ([a], [b], [c], [d], [e], [f], [g], [h], [i], [j], [k], [l], [m], [n], [o])
)
GO
CREATE TABLE [dbo].[dbx_codeanalysis_error_032_success2]
(
    [r]  VARCHAR          NOT NULL -- 1   byte    Default: VARCHAR(1)
  , [s]  VARCHAR(128)     NOT NULL -- 128 bytes	  
  , [t]  NVARCHAR         NOT NULL -- 2   bytes   Default: NVARCHAR(1)
  , [u]  NVARCHAR(128)    NOT NULL -- 256 bytes
  , [v]  SYSNAME          NOT NULL -- 256 bytes   VARCHAR(256)
  , [w]  UNIQUEIDENTIFIER NOT NULL -- 16  bytes	  
  , [x]  BINARY           NOT NULL -- 1   byte    Default: BINARY(1)
  , [y]  BINARY(2)        NOT NULL -- 2   bytes	  
  , [z]  VARBINARY        NOT NULL -- 1   byte    Default: VARBINARY(1)
  , [aa] VARBINARY(237)   NOT NULL -- 237 bytes
  , [ab] VARBINARY(1700)  NOT NULL
  , [ac] VARBINARY(1700)  NOT NULL
  , CONSTRAINT [PK_dbx_codeanalysis_error_032_success2] PRIMARY KEY ([r], [s], [t], [u], [v], [w], [x], [y], [z], [aa])
  , CONSTRAINT [UQ_dbx_codeanalysis_error_032_success2_ab] UNIQUE ([ab])
  , INDEX [IX_dbx_codeanalysis_error_032_success2_ac] ([ac])
)
GO