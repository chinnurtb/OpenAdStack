SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		richa
-- Create date: 2013-03-15
-- Description:	Update Azure entity status
-- =============================================
USE IndexDatastore;
IF OBJECT_ID('dbo.UpdateEntityStatus') IS NULL
	EXEC('CREATE PROCEDURE dbo.UpdateEntityStatus AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.UpdateEntityStatus
	@EntityIdList dbo.EntityIdListParam READONLY,
	@Active BIT
AS
BEGIN
	SET NOCOUNT ON;

	-- Update the entity index entry
	UPDATE entityId
	SET entityId.Active = @Active
	FROM dbo.EntityId AS entityId JOIN @EntityIdList AS ids
	ON entityId.xId = ids.ExternalEntityId

	SELECT @@ROWCOUNT AS UpdatedEntityCount;
	RETURN;
END
GO
