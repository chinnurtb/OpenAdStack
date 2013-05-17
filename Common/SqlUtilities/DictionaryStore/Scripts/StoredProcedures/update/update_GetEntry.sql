SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		bwoodall
-- Create date: 2012-06-07
-- Description:	Get an entry from Entry table
-- =============================================
USE DictionaryStore;
IF OBJECT_ID('dbo.GetEntry') IS NULL
	EXEC('CREATE PROCEDURE dbo.GetEntry AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.GetEntry
	@StoreName VARCHAR(64),
	@EntryName VARCHAR(64)
AS
BEGIN
	SET NOCOUNT ON;
	
	SELECT
		Content AS 'Content',
		ETag AS 'ETag',
		Compressed AS 'Compressed'
	FROM [Entry]
	WHERE
		StoreName = @StoreName AND
		Entryname = @EntryName;
	
	RETURN;
END
GO
