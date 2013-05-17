SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		richa
-- Create date: 2012-02-15
-- Description:	Get StorageType from StorageAccountName
-- =============================================
USE IndexDatastore;
IF OBJECT_ID('dbo.StorageAccount_GetStorageType') IS NULL
	EXEC('CREATE PROCEDURE dbo.StorageAccount_GetStorageType AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.StorageAccount_GetStorageType
	@StorageAccountName VARCHAR(120),
	@StorageType VARCHAR(20) OUTPUT
AS
BEGIN
	SET NOCOUNT ON;

	SELECT @StorageType = StorageType 
	FROM dbo.StorageAccount
	WHERE StorageAccountName = @StorageAccountName
	
	RETURN;
END
GO
