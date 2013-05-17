SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		richa
-- Create date: 2013-03-06
-- Description:	Update Azure entity index entry
-- =============================================
USE IndexDatastore;
IF OBJECT_ID('dbo.UpdateEntityIndexEntry') IS NULL
	EXEC('CREATE PROCEDURE dbo.UpdateEntityIndexEntry AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.UpdateEntityIndexEntry
	@ExternalEntityId UNIQUEIDENTIFIER,
	@Version INT,
	@TimeStamp DATETIME,
	@StorageAccountName VARCHAR(120),
	@TableName VARCHAR(120),
	@Partition VARCHAR(120),
	@RowId UNIQUEIDENTIFIER,
	@AssociationList dbo.AssociationListParam READONLY,
	@LastModifiedUser NVARCHAR(120),
	@SchemaVersion INT,
	@ExternalName NVARCHAR(120),
	@ExternalType NVARCHAR(120),
	@EntityCategory NVARCHAR(120),
	@Active BIT
AS
BEGIN
	SET NOCOUNT ON;
	
	-- Adding the record to the key fields table and updating the entity index must succeed together.
	-- Either can fail if an intervening update has caused the entity version to roll to the version
	-- we are trying to add.
	BEGIN TRANSACTION UpdateEntityIndex

	-- For now we always update storage type
	DECLARE @StorageType VARCHAR(20);
	EXEC dbo.StorageAccount_GetStorageType @StorageAccountName, @StorageType OUTPUT;

	-- Add new entry to the key fields table.
	-- Set up a table variable to receive the result that we don't want in our own result
	DECLARE @DropItOnTheFloor TABLE (
		xId UNIQUEIDENTIFIER,
		LocalVersion INT,
		StorageAccountName VARCHAR(120), 
		TableName VARCHAR(120), 
		Partition VARCHAR(120), 
		RowId UNIQUEIDENTIFIER,
		VersionTimestamp DATETIME);
	INSERT @DropItOnTheFloor EXEC dbo.KeyFields_InsertAzureKeyFields @ExternalEntityId, @StorageAccountName, @TableName, @Partition, @RowId, @Version, @TimeStamp
	
	IF NOT @@ERROR = 0
	BEGIN
		ROLLBACK TRANSACTION UpdateEntityIndex
		RETURN;
	END

	-- Add associations to association cache.
	-- Set up a table variable to receive the result that we don't want in our own result
	DECLARE @DropItOnTheFloorAssoc TABLE(
		xId uniqueidentifier,
		xIdTarget uniqueidentifier,
		Version int,
		ExternalName nvarchar(120),
		AssociationType varchar(15),
		Details nvarchar(240) NULL
	);
	INSERT @DropItOnTheFloorAssoc EXEC dbo.UpdateAssociationsCurrent @ExternalEntityId, @AssociationList

	IF NOT @@ERROR = 0
	BEGIN
		ROLLBACK TRANSACTION UpdateEntityIndex
		RETURN;
	END
	
	DECLARE @EntityExists INT;
	SELECT @EntityExists = COUNT(*) FROM dbo.EntityId WHERE xId = @ExternalEntityId;

	IF @EntityExists = 0
		BEGIN
			-- Insert the entity index entry
			DECLARE @InitialVersion INT = 0;
			DECLARE @InitialActive BIT = 1;
			DECLARE @WriteLock BIT = 0;
			INSERT INTO dbo.EntityId (xId, StorageType, Version, CreateDate, LastModifiedDate, WriteLock, HomeStorageAccountName,
				LastModifiedUser, SchemaVersion, ExternalName, ExternalType, EntityCategory, Active)
			VALUES (@ExternalEntityId, @StorageType, @InitialVersion, @TimeStamp, @TimeStamp, @WriteLock, @StorageAccountName,
				 @LastModifiedUser, @SchemaVersion, @ExternalName, @ExternalType, @EntityCategory, @InitialActive)
		END
	ELSE
		BEGIN
			-- If we are incrementing the version by more than one an intervening update has occured on this record
			-- and we should fail
			DECLARE @CurrentVersion INT;
			SELECT @CurrentVersion = @Version - 1;

			-- Update the entity index entry
			UPDATE dbo.EntityId
			SET Version = @Version, LastModifiedDate = @TimeStamp, HomeStorageAccountName = @StorageAccountName, StorageType = @StorageType,
				LastModifiedUser = @LastModifiedUser, SchemaVersion = @SchemaVersion, ExternalName = @ExternalName, ExternalType = @ExternalType,
				EntityCategory = @EntityCategory, Active = @Active
			WHERE xId = @ExternalEntityId AND Version = @CurrentVersion;
		END
	
	IF @@ROWCOUNT = 0
	BEGIN
		ROLLBACK TRANSACTION UpdateEntityIndex
		RAISERROR ('Update failed to modify the entity index record.', 16, 1);
		RETURN;
	END
	
	COMMIT TRANSACTION UpdateEntityIndex

	RETURN;
END
GO
