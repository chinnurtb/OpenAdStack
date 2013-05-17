SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		bwoodall
-- Create date: 2012-06-07
-- Description:	Upsert an entry to Entry table
-- =============================================
USE DictionaryStore;
IF OBJECT_ID('dbo.SetEntry') IS NULL
	EXEC('CREATE PROCEDURE dbo.SetEntry AS BEGIN SET NOCOUNT ON; END')
GO

ALTER PROCEDURE dbo.SetEntry
	@StoreName  VARCHAR(64),
	@EntryName  VARCHAR(64),
	@Content    VARBINARY(MAX),
	@Compressed BIT,
	@ETag       UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;

	BEGIN TRANSACTION UpsertWithETag

		-- UPSERT the entry
		IF EXISTS (
			SELECT *
			FROM [Entry]
			with (updlock, rowlock, holdlock)
			WHERE 
			StoreName = @StoreName AND
			Entryname = @EntryName)
		BEGIN
			-- Check eTag before updating
			DECLARE @CurrentETag UNIQUEIDENTIFIER
			SELECT @CurrentETag = ETag
			FROM [Entry]
			WHERE
				StoreName = @StoreName AND
				Entryname = @EntryName;

			IF NOT @ETag = @CurrentETag
			BEGIN
				DECLARE @ProvidedETag NVARCHAR(38)
				DECLARE @ExpectedETag NVARCHAR(38)
				SET @ProvidedETag = CONVERT(NVARCHAR(38), @ETag)
				SET @ExpectedETag = CONVERT(NVARCHAR(38), @CurrentETag)
				RAISERROR (
					N'The eTag %s for entry %s of %s is not valid (current eTag: %s)',
					16,
					1,
					@ProvidedETag,
					@EntryName,
					@StoreName,
					@ExpectedETag);
			END

			-- Update content and generate a new eTag
			UPDATE [Entry]
			SET
				Content = @Content,
				Compressed = @Compressed,
				ETag = NEWID()
			WHERE
				StoreName = @StoreName AND
				EntryName = @EntryName;
		END
		ELSE
		BEGIN
			-- Insert a new entry and generate an eTag
			INSERT [Entry]
				(StoreName, EntryName, Content, Compressed, ETag)
			VALUES
				(@StoreName, @EntryName, @Content, @Compressed, NEWID());
		END

		-- Return the entry's eTag
		SELECT ETag
		FROM [Entry]
		WHERE
			StoreName = @StoreName AND
			EntryName = @EntryName;

	COMMIT TRANSACTION UpsertWithETag;

	RETURN;
END
GO
