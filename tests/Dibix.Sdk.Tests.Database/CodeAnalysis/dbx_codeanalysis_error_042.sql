CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_042_options]
AS
BEGIN
    DECLARE @context VARBINARY(128)
	SET ARITHABORT                        ON       -- Fail => Unsupported option => Default is ON, OFF should not be used for performance reasons
    SET DATEFORMAT                        MDY      -- Fail => Unsupported option => Unknown
    SET FIPS_FLAGGER                      OFF      -- Fail => Unsupported option => Unknown
    SET ERRLVL                            0        -- Fail => Unsupported option
    SET NOEXEC                            OFF      -- Fail => Unsupported option
    SET OFFSETS SELECT, FROM, EXECUTE     ON       -- Fail => Unsupported option
    SET ROWCOUNT                          0        -- Fail => Unsupported option => Default is 0
    SET STATISTICS TIME                   ON       -- Fail => Unsupported option
    SET TEXTSIZE                          0        -- Fail => Unsupported option
    SETUSER                               N'x'     -- Fail => Unsupported option
    SET DEADLOCK_PRIORITY                 HIGH     -- Fail => Unsupported setting value
    SET NOCOUNT                           OFF      -- Fail => Unsupported setting value
    SET XACT_ABORT                        OFF      -- Fail => Unsupported setting value
    SET CONTEXT_INFO                      @context -- Success
    SET DEADLOCK_PRIORITY                 LOW      -- Success
    SET IDENTITY_INSERT [dbo].[dbx_table] OFF      -- Success
    SET IDENTITY_INSERT [dbo].[dbx_table] ON       -- Success
    SET NOCOUNT                           ON       -- Success
    SET TRANSACTION ISOLATION LEVEL       SNAPSHOT -- Success
    SET XACT_ABORT                        ON       -- Success
END
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_042_xact_fail] -- No TRY..CATCH, no SET XACT_ABORT ON
AS
BEGIN
    BEGIN TRANSACTION
    COMMIT TRANSACTION
END
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_042_xact_fai2] -- TRY..CATCH, but no ROLLBACK in CATCH
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION
        COMMIT TRANSACTION
    END TRY
    BEGIN CATCH
        ;THROW
    END CATCH
END
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_042_xact_fail3]
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION
            BEGIN TRY
                BEGIN TRANSACTION
                COMMIT TRANSACTION
            END TRY
            BEGIN CATCH -- The error is ignored, and not bubbled up. Therefore the ROLLBACK at line 45 won't catch the TRANSACTION initiated at line 36.
            END CATCH
        COMMIT TRANSACTION
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION
        ;THROW
    END CATCH
END
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_042_xact_success1]
AS
BEGIN
    SET XACT_ABORT ON

    BEGIN TRANSACTION
    COMMIT TRANSACTION
END
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_042_xact_success2]
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION
        COMMIT TRANSACTION
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION
        ;THROW
    END CATCH
END
GO
CREATE PROCEDURE [dbo].[dbx_codeanalysis_error_042_xact_success3]
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION
            BEGIN TRY
                BEGIN TRANSACTION
                COMMIT TRANSACTION
            END TRY
            BEGIN CATCH
                ;THROW
            END CATCH
        COMMIT TRANSACTION
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION
        ;THROW
    END CATCH
END