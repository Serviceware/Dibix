CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_032]
AS
	CREATE TABLE [dbo].[dbx_codeanalysis_error_032_fail]
	(
		[a]  BIT              NOT NULL -- 1   byte
	  , [b]  TINYINT          NOT NULL -- 1   byte
	  , [c]  SMALLINT         NOT NULL -- 2   bytes
	  , [d]  INT              NOT NULL -- 4   bytes
	  , [e]  BIGINT           NOT NULL -- 8   bytes
	  , [f]  DECIMAL          NOT NULL -- 9   bytes   => Default: DECIMAL(18,0) 
	  , [g]  DECIMAL(9)       NOT NULL -- 5   bytes   Precision 1-9   => 5  storage bytes
	  , [h]  DECIMAL(9,5)     NOT NULL -- 5   bytes   Precision 1-9   => 5  storage bytes
	  , [i]  DECIMAL(10,5)    NOT NULL -- 9   bytes   Precision 10-19 => 9  storage bytes
	  , [j]  DECIMAL(20,10)   NOT NULL -- 13  bytes   Precision 20-28 => 13 storage bytes
	  , [k]  DECIMAL(29,15)   NOT NULL -- 17  bytes   Precision 29-38 => 17 storage bytes
	  , [l]  DATE             NOT NULL -- 4   bytes	  
	  , [m]  DATETIME         NOT NULL -- 8   bytes	  
	  , [n]  CHAR             NOT NULL -- 1   byte    Default: CHAR(1)
	  , [o]  CHAR(10)         NOT NULL -- 10  bytes	  
	  , [p]  NCHAR            NOT NULL -- 2   bytes   Default: NCHAR(1)
	  , [q]  NCHAR(10)        NOT NULL -- 20  bytes	  
	  , [r]  VARCHAR          NOT NULL -- 1   byte    Default: VARCHAR(1)
	  , [s]  VARCHAR(256)     NOT NULL -- 256 bytes	  
	  , [t]  NVARCHAR         NOT NULL -- 2   bytes   Default: NVARCHAR(1)
	  , [u]  NVARCHAR(256)    NOT NULL -- 512 bytes
	  , [v]  SYSNAME          NOT NULL -- 256 bytes   VARCHAR(256)
	  , [w]  UNIQUEIDENTIFIER NOT NULL -- 16  bytes	  
	  , [x]  BINARY           NOT NULL -- 1   byte    Default: BINARY(1)
	  , [y]  BINARY(2)        NOT NULL -- 2   bytes	  
	  , [z]  VARBINARY        NOT NULL -- 1   byte    Default: VARBINARY(1)
	  , [aa] VARBINARY(2)     NOT NULL -- 2   bytes
	  , [ab] VARBINARY(1701)  NOT NULL
	  , [ac] VARBINARY(1701)  NOT NULL
	  , CONSTRAINT [PK_dbx_codeanalysis_error_032_fail] PRIMARY KEY ([a], [b], [c], [d], [e], [f], [g], [h], [i], [j], [k], [l], [m], [n], [o], [p], [q], [r], [s], [t], [u], [v], [w], [x], [y], [z], [aa])
	  , CONSTRAINT [UQ_dbx_codeanalysis_error_032_fail_ab] UNIQUE ([ab])
	  , INDEX [IX_dbx_codeanalysis_error_032_fail_ac] ([ac])
	)
	
	CREATE TABLE [dbo].[dbx_codeanalysis_error_032_success]
	(
		[a]  BIT              NOT NULL -- 1   byte
	  , [b]  TINYINT          NOT NULL -- 1   byte
	  , [c]  SMALLINT         NOT NULL -- 2   bytes
	  , [d]  INT              NOT NULL -- 4   bytes
	  , [e]  BIGINT           NOT NULL -- 8   bytes
	  , [f]  DECIMAL          NOT NULL -- 9   bytes   => Default: DECIMAL(18,0) 
	  , [g]  DECIMAL(9)       NOT NULL -- 5   bytes   Precision 1-9   => 5  storage bytes
	  , [h]  DECIMAL(9,5)     NOT NULL -- 5   bytes   Precision 1-9   => 5  storage bytes
	  , [i]  DECIMAL(10,5)    NOT NULL -- 9   bytes   Precision 10-19 => 9  storage bytes
	  , [j]  DECIMAL(20,10)   NOT NULL -- 13  bytes   Precision 20-28 => 13 storage bytes
	  , [k]  DECIMAL(29,15)   NOT NULL -- 17  bytes   Precision 29-38 => 17 storage bytes
	  , [l]  DATE             NOT NULL -- 4   bytes	  
	  , [m]  DATETIME         NOT NULL -- 8   bytes	  
	  , [n]  CHAR             NOT NULL -- 1   byte    Default: CHAR(1)
	  , [o]  CHAR(10)         NOT NULL -- 10  bytes	  
	  , [p]  NCHAR            NOT NULL -- 2   bytes   Default: NCHAR(1)
	  , [q]  NCHAR(10)        NOT NULL -- 20  bytes	  
	  , [r]  VARCHAR          NOT NULL -- 1   byte    Default: VARCHAR(1)
	  , [s]  VARCHAR(256)     NOT NULL -- 256 bytes	  
	  , [t]  NVARCHAR         NOT NULL -- 2   bytes   Default: NVARCHAR(1)
	  , [u]  NVARCHAR(122)    NOT NULL -- 512 bytes
	  , [v]  SYSNAME          NOT NULL -- 256 bytes   VARCHAR(256)
	  , [w]  UNIQUEIDENTIFIER NOT NULL -- 16  bytes	  
	  , [x]  BINARY           NOT NULL -- 1   byte    Default: BINARY(1)
	  , [y]  BINARY(2)        NOT NULL -- 2   bytes	  
	  , [z]  VARBINARY        NOT NULL -- 1   byte    Default: VARBINARY(1)
	  , [aa] VARBINARY(2)     NOT NULL -- 2   bytes
	  , [ab] VARBINARY(1700)  NOT NULL
	  , [ac] VARBINARY(1700)  NOT NULL
	  , CONSTRAINT [PK_dbx_codeanalysis_error_032_success] PRIMARY KEY ([a], [b], [c], [d], [e], [f], [g], [h], [i], [j], [k], [l], [m], [n], [o], [p], [q], [r], [s], [t], [u], [v], [w], [x], [y], [z], [aa])
	  , CONSTRAINT [UQ_dbx_codeanalysis_error_032_success_ab] UNIQUE ([ab])
	  , INDEX [IX_dbx_codeanalysis_error_032_success_ac] ([ac])
	)