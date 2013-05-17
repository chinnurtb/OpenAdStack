SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		richa
-- Create date: 2012-02-15
-- Description:	Insert Azure entity key fields
-- =============================================
USE IndexDatastore;
IF OBJECT_ID('dbo.KeyFields_InsertAzureKeyFields') IS NULL
	EXEC('CREATE PROCEDURE dbo.KeyFields_InsertAzureKeyFields AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.KeyFields_InsertAzureKeyFields
	@ExternalEntityId UNIQUEIDENTIFIER,
	@StorageAccountName VARCHAR(120),
	@TableName VARCHAR(120),
	@Partition VARCHAR(120),
	@RowId UNIQUEIDENTIFIER,
	@LocalVersion INT,
	@VersionTimestamp DATETIME
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.AzureKeyFieldsVersioned (xId, StorageAccountName, TableName, Partition, RowId, LocalVersion, VersionTimestamp)
	VALUES (@ExternalEntityId, @StorageAccountName, @TableName, @Partition, @RowId, @LocalVersion, @VersionTimestamp)

	IF @@ROWCOUNT = 0
	BEGIN
		RAISERROR ('Failed to insert the key fields record.', 16, 1);
		RETURN;
	END
	
	SELECT
		xId AS 'ExternalEntityId',
		LocalVersion AS 'LocalVersion',
		StorageAccountName AS 'StorageAccountName', 
		TableName AS 'TableName', 
		Partition AS 'Partition', 
		RowId AS 'RowId',
		VersionTimestamp AS 'VersionTimestamp'
	FROM dbo.AzureKeyFieldsVersioned
	WHERE xId = @ExternalEntityId AND StorageAccountName = @StorageAccountName AND LocalVersion = @LocalVersion
	
	RETURN;
END
GO
