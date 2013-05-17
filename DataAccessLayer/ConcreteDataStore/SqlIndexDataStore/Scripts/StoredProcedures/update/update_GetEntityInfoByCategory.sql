SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		richa
-- Create date: 2013-03-20
-- Description:	Get entity info by EntityCategory
-- =============================================

USE IndexDatastore;
IF OBJECT_ID('dbo.GetEntityInfoByCategory') IS NULL
	EXEC('CREATE PROCEDURE dbo.GetEntityInfoByCategory AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.GetEntityInfoByCategory
	@EntityCategory NVARCHAR(120)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT  xId AS 'ExternalEntityId', EntityCategory AS 'EntityCategory', ExternalName AS 'ExternalName', ExternalType AS 'ExternalType'
	FROM dbo.EntityId where EntityCategory = @EntityCategory AND xId <> '00000000-0000-0000-0000-000000000001' AND Active = 1
	
	RETURN;
END
