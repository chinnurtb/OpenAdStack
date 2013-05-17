SET QUOTED_IDENTIFIER OFF;
GO
USE [DictionaryStore];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'Entry'
CREATE TABLE [dbo].[Entry] (
    [StoreName] varchar(64)  NOT NULL,
	[EntryName] varchar(64)  NOT NULL,
	[Content] varbinary(max)  NOT NULL,
	[ETag] uniqueidentifier  NOT NULL,
	[Compressed] bit  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [EntryName], [StoreName] in table 'Entry'
ALTER TABLE [dbo].[Entry]
ADD CONSTRAINT [PK_Entry]
    PRIMARY KEY CLUSTERED ([EntryName], [StoreName] ASC);
GO

-- --------------------------------------------------
-- Creating all indexes
-- --------------------------------------------------

-- Create an index on [EntryName], [StoreName]
CREATE UNIQUE INDEX IX_Entry_EntryNameStoreName
	ON [dbo].[Entry] ([EntryName], [StoreName] ASC);

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------
