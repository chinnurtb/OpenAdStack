-- --------------------------------------------------
-- Creating static data
-- --------------------------------------------------
USE [IndexDatastore];

DECLARE @DefaultAzureAccount INT;
SET @DefaultAzureAccount = (SELECT COUNT(*) 
	FROM dbo.StorageAccount 
	WHERE StorageAccountName = 'DefaultAzureStorageAccount');

IF (@DefaultAzureAccount = 0)
BEGIN
	INSERT into [dbo].[StorageAccount] ([StorageAccountName], [StorageAddress], [StorageType])
	VALUES ('DefaultAzureStorageAccount', 'NotCurrentlyUsed', 'AzureTable')
END
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------
