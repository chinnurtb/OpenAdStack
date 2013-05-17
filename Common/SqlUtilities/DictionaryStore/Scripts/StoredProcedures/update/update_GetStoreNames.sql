SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		bwoodall
-- Create date: 2012-06-07
-- Description:	Get the names of entries from Entry table
-- =============================================
USE DictionaryStore;
IF OBJECT_ID('dbo.GetStoreNames') IS NULL
	EXEC('CREATE PROCEDURE dbo.GetStoreNames AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.GetStoreNames
AS
BEGIN
	SET NOCOUNT ON;
	
	SELECT DISTINCT StoreName AS 'StoreName'
	FROM [Entry];
	
	RETURN;
END
GO