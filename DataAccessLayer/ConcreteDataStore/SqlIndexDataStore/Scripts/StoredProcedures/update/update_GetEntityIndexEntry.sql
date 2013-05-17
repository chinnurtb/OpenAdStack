SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		richa
-- Create date: 2013-03-06
-- Description:	Get the current version of the entity data, key fields and cached associations.
-- (Can return key fields for multiple storage accounts if the same version exists
-- on multiple accounts).
-- =============================================
USE IndexDatastore;
IF OBJECT_ID('dbo.GetEntityIndexEntry') IS NULL
	EXEC('CREATE PROCEDURE dbo.GetEntityIndexEntry AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.GetEntityIndexEntry
	@ExternalEntityId UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;

	-- The purpose of the transaction in this case is to guard consistency
	-- between the version in EntityId table and Associations table
	SET TRANSACTION ISOLATION LEVEL REPEATABLE READ
	BEGIN TRANSACTION EntityCurrentVersion

    -- Get indexed properties	
	SELECT 
		xId AS 'ExternalEntityId', 
		StorageType AS 'StorageType', 
		Version AS 'Version', 
		CreateDate AS 'CreateDate', 
		LastModifiedDate AS 'LastModifiedDate', 
		HomeStorageAccountName AS 'HomeStorageAccountName',
		LastModifiedUser AS 'LastModifiedUser', 
		SchemaVersion AS 'SchemaVersion',
		ExternalName AS 'ExternalName',
		EntityCategory AS 'EntityCategory',
		ExternalType AS 'ExternalType',
		Active AS 'Active'
	FROM dbo.EntityId WHERE xId = @ExternalEntityId

	-- Gets current version key fields (null for version)
	EXEC dbo.GetEntityKeyFieldsForVersion @ExternalEntityId, NULL;

	-- Get associations
	EXEC dbo.GetAssociationsCurrent @ExternalEntityId;

	COMMIT TRANSACTION EntityCurrentVersion

	-- Return data sets for the key fields and the associations
	RETURN;
END
GO
