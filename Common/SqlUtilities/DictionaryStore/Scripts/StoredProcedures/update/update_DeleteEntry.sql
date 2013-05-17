SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		bwoodall
-- Create date: 2012-06-07
-- Description:	Delete single entry from Entry table
-- =============================================
USE DictionaryStore;
IF OBJECT_ID('dbo.DeleteEntry') IS NULL
	EXEC('CREATE PROCEDURE dbo.DeleteEntry AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.DeleteEntry
	@StoreName VARCHAR(64),
	@EntryName VARCHAR(64)
AS
BEGIN
	SET NOCOUNT ON;
	
	DELETE [Entry]
	WHERE
		StoreName = @StoreName AND
		Entryname = @EntryName;
	
	RETURN;
END
GO
