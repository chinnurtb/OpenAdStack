SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		richa
-- Create date: 2012-07-02
-- Description:	Remove User access
-- =============================================
USE IndexDatastore;
IF OBJECT_ID('dbo.UserAccess_RemoveUserAccess') IS NULL
	EXEC('CREATE PROCEDURE dbo.UserAccess_RemoveUserAccess AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.UserAccess_RemoveUserAccess
	@ExternalEntityId UNIQUEIDENTIFIER,
	@AccessDescriptor NVARCHAR(240)
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM dbo.UserAccess
	WHERE xId = @ExternalEntityId and AccessDescriptor = @AccessDescriptor;
END
GO
