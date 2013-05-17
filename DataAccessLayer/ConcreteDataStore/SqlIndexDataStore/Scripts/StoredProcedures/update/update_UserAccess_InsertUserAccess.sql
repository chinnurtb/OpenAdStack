SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		richa
-- Create date: 2012-07-02
-- Description:	Insert user access descriptor
-- =============================================
USE IndexDatastore;
IF OBJECT_ID('dbo.UserAccess_InsertUserAccess') IS NULL
	EXEC('CREATE PROCEDURE dbo.UserAccess_InsertUserAccess AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.UserAccess_InsertUserAccess
	@ExternalEntityId UNIQUEIDENTIFIER,
	@AccessDescriptor NVARCHAR(240)
AS
BEGIN
	SET NOCOUNT ON;

	BEGIN TRANSACTION CheckForDupe

		INSERT INTO dbo.UserAccess (xId, AccessDescriptor)
		VALUES (@ExternalEntityId, @AccessDescriptor)

		-- Optimistic insert so check count. If there was a race
		-- condition one of them will see more than one row and should
		-- roll back.
		DECLARE @Count INT = 0;
		SELECT @Count = (SELECT COUNT(*) FROM dbo.UserAccess
		WHERE xId = @ExternalEntityId and AccessDescriptor = @AccessDescriptor);

	IF @Count > 1
	BEGIN
		ROLLBACK TRANSACTION CheckForDupe
		SELECT @Count = 0;
	END
	ELSE
		COMMIT TRANSACTION CheckForDupe
END
GO
