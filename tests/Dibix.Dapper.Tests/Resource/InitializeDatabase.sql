CREATE TYPE [dbo].[_dibix_tests_structuredtype] AS TABLE
(
    [intvalue]     INT           NOT NULL
  , [stringvalue]  NVARCHAR(MAX) NOT NULL
  , [decimalvalue] DECIMAL(14,2) NOT NULL
  , PRIMARY KEY ([intvalue])
)
GO
CREATE PROCEDURE [dbo].[_dibix_tests_sp1] @out1 INT OUTPUT, @out2 BIT OUTPUT, @out3 NVARCHAR(50) OUTPUT
AS
BEGIN
    SET @out1 = 5
    SET @out2 = 1
    SET @out3 = N'x'
END