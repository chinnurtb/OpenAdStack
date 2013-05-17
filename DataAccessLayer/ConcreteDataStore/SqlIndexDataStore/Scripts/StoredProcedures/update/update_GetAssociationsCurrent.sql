SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		richa
-- Create date: 2013-03-05
-- Description:	Get the current associations of an entity.
-- =============================================
USE IndexDatastore;
IF OBJECT_ID('dbo.GetAssociationsCurrent') IS NULL
	EXEC('CREATE PROCEDURE dbo.GetAssociationsCurrent AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.GetAssociationsCurrent
	@ExternalEntityId UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;

	SELECT 
		assoc.xId AS 'ExternalEntityId',
		assoc.xIdTarget AS 'TargetEntityId',
		assoc.ExternalName AS 'ExternalName',
		assoc.AssociationType AS 'AssociationType',
		assoc.Details AS 'Details',
		entityId.EntityCategory AS 'TargetEntityCategory',
		entityId.ExternalType AS 'TargetExternalType'
	FROM dbo.Associations AS assoc JOIN dbo.EntityId AS entityId
	ON assoc.xIdTarget = entityId.xId
	WHERE assoc.xId = @ExternalEntityId AND entityId.Active = 1

	RETURN;
END
GO
