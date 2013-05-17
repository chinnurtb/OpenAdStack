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
IF OBJECT_ID('dbo.GetEntryNames') IS NULL
	EXEC('CREATE PROCEDURE dbo.GetEntryNames AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.GetEntryNames
	@StoreName VARCHAR(64)
AS
BEGIN
	SET NOCOUNT ON;
	
	SELECT
		EntryName AS 'EntryName'
	FROM [Entry]
	WHERE StoreName = @StoreName;
	
	RETURN;
END
GO