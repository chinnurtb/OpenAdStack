SET QUOTED_IDENTIFIER OFF;
GO
USE [DictionaryStore];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing tables and indexes
-- --------------------------------------------------

IF EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_Entry_EntryNameStoreName')
	DROP INDEX IX_Entry_EntryNameStoreName ON [dbo].[Entry]
GO

IF OBJECT_ID(N'[dbo].[Entry]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Entry];
GO
