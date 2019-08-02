CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_032]
AS
	CREATE TABLE [dbx_codeanalysis_error_032_fail]
	(
		[1]  BIT              NOT NULL -- 1   byte
	  , [2]  TINYINT          NOT NULL -- 1   byte
	  , [3]  SMALLINT         NOT NULL -- 2   bytes
	  , [4]  INT              NOT NULL -- 4   bytes
	  , [5]  BIGINT           NOT NULL -- 8   bytes
	  , [6]  DECIMAL          NOT NULL -- 9   bytes   => Default: DECIMAL(18,0) 
	  , [7]  DECIMAL(9)       NOT NULL -- 5   bytes   Precision 1-9   => 5  storage bytes
	  , [8]  DECIMAL(9,5)     NOT NULL -- 5   bytes   Precision 1-9   => 5  storage bytes
	  , [9]  DECIMAL(10,5)    NOT NULL -- 9   bytes   Precision 10-19 => 9  storage bytes
	  , [10] DECIMAL(20,10)   NOT NULL -- 13  bytes   Precision 20-28 => 13 storage bytes
	  , [11] DECIMAL(29,15)   NOT NULL -- 17  bytes   Precision 29-38 => 17 storage bytes
	  , [12] DATE             NOT NULL -- 4   bytes	  
	  , [13] DATETIME         NOT NULL -- 8   bytes	  
	  , [14] CHAR             NOT NULL -- 1   byte    Default: CHAR(1)
	  , [15] CHAR(10)         NOT NULL -- 10  bytes	  
	  , [16] NCHAR            NOT NULL -- 2   bytes   Default: NCHAR(1)
	  , [17] NCHAR(10)        NOT NULL -- 20  bytes	  
	  , [18] VARCHAR          NOT NULL -- 1   byte    Default: VARCHAR(1)
	  , [19] VARCHAR(256)     NOT NULL -- 256 bytes	  
	  , [20] NVARCHAR         NOT NULL -- 2   bytes   Default: NVARCHAR(1)
	  , [21] NVARCHAR(256)    NOT NULL -- 512 bytes
	  , [22] SYSNAME          NOT NULL -- 256 bytes   VARCHAR(256)
	  , [23] UNIQUEIDENTIFIER NOT NULL -- 16  bytes	  
	  , [24] BINARY           NOT NULL -- 1   byte    Default: BINARY(1)
	  , [25] BINARY(2)        NOT NULL -- 2   bytes	  
	  , [26] VARBINARY        NOT NULL -- 1   byte    Default: VARBINARY(1)
	  , [27] VARBINARY(2)     NOT NULL -- 2   bytes
	  , CONSTRAINT [PK_dbx_codeanalysis_error_032_fail] PRIMARY KEY ([1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12], [13], [14], [15], [16], [17], [18], [19], [20], [21], [22], [23], [24], [25], [26], [27])
	)
	
	CREATE TABLE [dbx_codeanalysis_error_032_success]
	(
		[1]  BIT              NOT NULL -- 1   byte
	  , [2]  TINYINT          NOT NULL -- 1   byte
	  , [3]  SMALLINT         NOT NULL -- 2   bytes
	  , [4]  INT              NOT NULL -- 4   bytes
	  , [5]  BIGINT           NOT NULL -- 8   bytes
	  , [6]  DECIMAL          NOT NULL -- 9   bytes   => Default: DECIMAL(18,0) 
	  , [7]  DECIMAL(9)       NOT NULL -- 5   bytes   Precision 1-9   => 5  storage bytes
	  , [8]  DECIMAL(9,5)     NOT NULL -- 5   bytes   Precision 1-9   => 5  storage bytes
	  , [9]  DECIMAL(10,5)    NOT NULL -- 9   bytes   Precision 10-19 => 9  storage bytes
	  , [10] DECIMAL(20,10)   NOT NULL -- 13  bytes   Precision 20-28 => 13 storage bytes
	  , [11] DECIMAL(29,15)   NOT NULL -- 17  bytes   Precision 29-38 => 17 storage bytes
	  , [12] DATE             NOT NULL -- 4   bytes	  
	  , [13] DATETIME         NOT NULL -- 8   bytes	  
	  , [14] CHAR             NOT NULL -- 1   byte    Default: CHAR(1)
	  , [15] CHAR(10)         NOT NULL -- 10  bytes	  
	  , [16] NCHAR            NOT NULL -- 2   bytes   Default: NCHAR(1)
	  , [17] NCHAR(10)        NOT NULL -- 20  bytes	  
	  , [18] VARCHAR          NOT NULL -- 1   byte    Default: VARCHAR(1)
	  , [19] VARCHAR(256)     NOT NULL -- 256 bytes	  
	  , [20] NVARCHAR         NOT NULL -- 2   bytes   Default: NVARCHAR(1)
	  , [21] NVARCHAR(122)    NOT NULL -- 512 bytes
	  , [22] SYSNAME          NOT NULL -- 256 bytes   VARCHAR(256)
	  , [23] UNIQUEIDENTIFIER NOT NULL -- 16  bytes	  
	  , [24] BINARY           NOT NULL -- 1   byte    Default: BINARY(1)
	  , [25] BINARY(2)        NOT NULL -- 2   bytes	  
	  , [26] VARBINARY        NOT NULL -- 1   byte    Default: VARBINARY(1)
	  , [27] VARBINARY(2)     NOT NULL -- 2   bytes
	  , CONSTRAINT [PK_dbx_codeanalysis_error_032_success] PRIMARY KEY ([1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12], [13], [14], [15], [16], [17], [18], [19], [20], [21], [22], [23], [24], [25], [26], [27])
	)