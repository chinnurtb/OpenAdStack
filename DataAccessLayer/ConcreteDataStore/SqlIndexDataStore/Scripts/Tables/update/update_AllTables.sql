SET QUOTED_IDENTIFIER OFF;
GO
USE [IndexDatastore];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'Associations' if it doesn't exist
CREATE TABLE [dbo].[Associations] (
	[indexId] bigint  IDENTITY(1,1),
	[xId] uniqueidentifier NOT NULL,
	[xIdTarget] uniqueidentifier NOT NULL,
	[ExternalName] nvarchar(120) NOT NULL,
	[AssociationType] varchar(15) NOT NULL,
	[Details] nvarchar(240) NULL
);
GO

-- Create a type for passing Associations lists as parameters to stored procedures
CREATE TYPE [dbo].[AssociationListParam] AS TABLE 
(
	[ExternalName] nvarchar(120) NULL,
	[TargetEntityId] uniqueidentifier NOT NULL,
	[AssociationType] varchar(15) NOT NULL,
	[Details] nvarchar(240) NULL
);
GO

-- Creating new version of AzureKeyFields table 'AzureKeyFieldsVersioned'
CREATE TABLE [dbo].[AzureKeyFieldsVersioned] (
	[indexId] bigint IDENTITY(1,1), 
    [xId] uniqueidentifier  NOT NULL,
    [StorageAccountName] varchar(120)  NOT NULL,
    [TableName] varchar(120)  NULL,
    [Partition] varchar(120)  NULL,
    [RowId] uniqueidentifier  NOT NULL,
    [LocalVersion] int  NOT NULL,
	[VersionTimestamp] datetime NULL
);
GO

-- Creating table 'EntityId'
CREATE TABLE [dbo].[EntityId] (
    [xId] uniqueidentifier  NOT NULL,
    [StorageType] varchar(20)  NOT NULL,
    [Version] int  NOT NULL,
    [CreateDate] datetime  NOT NULL,
    [LastModifiedDate] datetime  NOT NULL,
    [WriteLock] bit  NOT NULL,
    [HomeStorageAccountName] varchar(120)  NOT NULL,
	[LastModifiedUser] nvarchar(120) NULL,
	[SchemaVersion] int NULL,
	[ExternalName] nvarchar(120) NULL,
	[EntityCategory] nvarchar(120) NULL,
	[ExternalType] nvarchar(120) NULL,
	[Active] bit NOT NULL CONSTRAINT DF_EntityId_Active DEFAULT 1
);
GO

-- Create a type for passing EntityId lists as parameters to stored procedures
CREATE TYPE [dbo].[EntityIdListParam] AS TABLE 
(
	[ExternalEntityId] uniqueidentifier NOT NULL
);
GO

-- Creating table 'S3KeyFields'
CREATE TABLE [dbo].[S3KeyFields] (
    [xId] uniqueidentifier  NOT NULL,
    [StorageAccountName] varchar(120)  NOT NULL,
    [TBDS3] varchar(max)  NULL,
    [LocalVersion] int  NOT NULL
);
GO

-- Creating table 'StorageAccount'
CREATE TABLE [dbo].[StorageAccount] (
    [StorageAccountName] varchar(120)  NOT NULL,
    [StorageAddress] varchar(750)  NOT NULL,
    [StorageType] varchar(20)  NULL
);
GO

-- Creating table 'XmlKeyFields'
CREATE TABLE [dbo].[XmlKeyFields] (
    [xId] uniqueidentifier  NOT NULL,
    [StorageAccountName] varchar(120)  NOT NULL,
    [TableName] varchar(120)  NULL,
    [Partition] varchar(120)  NULL,
    [RowId] uniqueidentifier  NOT NULL,
    [LocalVersion] int  NOT NULL
);
GO

-- Creating table 'UserAccess'
CREATE TABLE [dbo].[UserAccess] (
    [xId] uniqueidentifier  NOT NULL,
    [AccessDescriptor] varchar(240)  NOT NULL,
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [indexId] in table 'Associations'
ALTER TABLE [dbo].[Associations]
	ADD	CONSTRAINT [PK_Associations_New] PRIMARY KEY CLUSTERED ([indexId] ASC)
GO

-- Creating primary key clustered index on [indexId] in table 'AzureKeyFieldsVersioned'
ALTER TABLE [dbo].[AzureKeyFieldsVersioned]
ADD CONSTRAINT [PK_AzureKeyFieldsVersioned]
    PRIMARY KEY CLUSTERED ([indexId] ASC);
GO

-- Creating non-clustered index on [xId], [StorageAccountName], [LocalVersion] in table 'AzureKeyFieldsVersioned'
ALTER TABLE [dbo].[AzureKeyFieldsVersioned]
ADD CONSTRAINT [UK_AzureKeyFieldsVersioned]
    UNIQUE NONCLUSTERED ([xId], [StorageAccountName], [LocalVersion]);
GO

-- Creating primary key on [xId] in table 'EntityId'
ALTER TABLE [dbo].[EntityId]
ADD CONSTRAINT [PK_EntityId]
    PRIMARY KEY CLUSTERED ([xId] ASC);
GO

-- Creating primary key on [xId] in table 'S3KeyFields'
ALTER TABLE [dbo].[S3KeyFields]
ADD CONSTRAINT [PK_S3KeyFields]
    PRIMARY KEY CLUSTERED ([xId] ASC);
GO

-- Creating primary key on [StorageAccountName] in table 'StorageAccount'
ALTER TABLE [dbo].[StorageAccount]
ADD CONSTRAINT [PK_StorageAccount]
    PRIMARY KEY CLUSTERED ([StorageAccountName] ASC);
GO

-- Creating primary key on [xId] in table 'XmlKeyFields'
ALTER TABLE [dbo].[XmlKeyFields]
ADD CONSTRAINT [PK_XmlKeyFields]
    PRIMARY KEY CLUSTERED ([xId] ASC);
GO

-- --------------------------------------------------
-- Creating Indexes
-- --------------------------------------------------

-- Creating clustered index on [xId] in table 'UserAccess'
IF OBJECT_ID(N'[dbo].[IX_UserAccess]', 'U') IS NULL
    CREATE CLUSTERED INDEX IX_UserAccess ON [dbo].[UserAccess] (xId);
GO

-- Creating non-clustered indexs on table Associations on [xId] and [xIdTarget]
CREATE NONCLUSTERED INDEX [IX_Associations_xId] ON [dbo].[Associations] ([xId]);
GO
CREATE NONCLUSTERED	INDEX [IX_Associations_xIdTarget] ON [dbo].[Associations] ([xIdTarget]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------
