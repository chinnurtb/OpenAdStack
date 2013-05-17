SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		richa
-- Create date: 2013-03-20
-- Description:	Get entity key fields at a specific version.
-- (Can return key fields for multiple storage accounts if the same version exists
-- on multiple accounts).
-- =============================================
USE IndexDatastore;
IF OBJECT_ID('dbo.GetEntityKeyFieldsForVersion') IS NULL
	EXEC('CREATE PROCEDURE dbo.GetEntityKeyFieldsForVersion AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.GetEntityKeyFieldsForVersion
	@ExternalEntityId UNIQUEIDENTIFIER,
	@Version INT
AS
BEGIN
	SET NOCOUNT ON;

	-- If we have a NULL version get the current version, otherwise use the version provided
	DECLARE @VersionToGet INT;
	SELECT @VersionToGet = @Version;
	IF (@Version is NULL)
	BEGIN
		SELECT @VersionToGet = Version FROM dbo.EntityId WHERE xId = @ExternalEntityId;
	END

	SELECT 
		azureKeys.xId AS 'ExternalEntityId',
		azureKeys.LocalVersion AS 'LocalVersion',
		azureKeys.StorageAccountName AS 'StorageAccountName', 
		azureKeys.TableName AS 'TableName', 
		azureKeys.Partition AS 'Partition', 
		azureKeys.RowId AS 'RowId',
		azureKeys.VersionTimestamp AS 'VersionTimestamp'
	FROM dbo.EntityId AS entityIds JOIN dbo.AzureKeyFieldsVersioned AS azureKeys
		ON (entityIds.xId = azureKeys.xId)
	WHERE entityIds.xId = @ExternalEntityId AND azureKeys.LocalVersion = @VersionToGet

	RETURN;
END
GO
