SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		richa
-- Create date: 2012-07-02
-- Description:	Get User access by user entity Id
-- =============================================
USE IndexDatastore;
IF OBJECT_ID('dbo.UserAccess_GetUserAccess') IS NULL
	EXEC('CREATE PROCEDURE dbo.UserAccess_GetUserAccess AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.UserAccess_GetUserAccess
	@ExternalEntityId UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;
	
	SELECT AccessDescriptor AS 'AccessDescriptor' 
	FROM dbo.UserAccess
	WHERE xId = @ExternalEntityId
	
	RETURN;
END
GO
