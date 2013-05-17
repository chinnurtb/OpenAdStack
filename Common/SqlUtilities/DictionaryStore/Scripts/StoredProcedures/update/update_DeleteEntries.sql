SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		bwoodall
-- Create date: 2012-06-07
-- Description:	Delete entries from Entry table
-- =============================================
USE DictionaryStore;
IF OBJECT_ID('dbo.DeleteEntries') IS NULL
	EXEC('CREATE PROCEDURE dbo.DeleteEntries AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.DeleteEntries
	@StoreName VARCHAR(64)
AS
BEGIN
	SET NOCOUNT ON;

	DELETE [Entry]
	WHERE StoreName = @StoreName;
	
	RETURN;
END
GO
