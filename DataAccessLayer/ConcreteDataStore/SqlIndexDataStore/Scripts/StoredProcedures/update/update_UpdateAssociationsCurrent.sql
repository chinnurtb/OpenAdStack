SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		richa
-- Create date: 2013-03-05
-- Description:	Update the associations in the association cache.
-- =============================================
USE IndexDatastore;
IF OBJECT_ID('dbo.UpdateAssociationsCurrent') IS NULL
	EXEC('CREATE PROCEDURE dbo.UpdateAssociationsCurrent AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.UpdateAssociationsCurrent
	@ExternalEntityId UNIQUEIDENTIFIER,
	@AssociationList dbo.AssociationListParam READONLY
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM dbo.Associations WHERE xId = @ExternalEntityId;

	DECLARE @UpdateCount INT;
	SELECT @UpdateCount = COUNT(*) FROM @AssociationList;
	IF @UpdateCount = 0
		RETURN;

	INSERT INTO dbo.Associations SELECT 
		@ExternalEntityId AS xId, 
		TargetEntityId AS xIdTarget, 
		ExternalName AS ExternalName,
		AssociationType AS AssociationType,
		Details AS Details 
		FROM @AssociationList;

	IF @@ROWCOUNT = 0
	BEGIN
		RAISERROR ('Failed to insert the association cache records.', 16, 1);
		RETURN;
	END

	RETURN;
END
GO
