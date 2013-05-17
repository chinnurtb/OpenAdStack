SET QUOTED_IDENTIFIER OFF;
GO
USE [IndexDatastore];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[Associations]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Associations];
GO
IF OBJECT_ID(N'[dbo].[AzureKeyFields]', 'U') IS NOT NULL
    DROP TABLE [dbo].[AzureKeyFields];
GO
IF OBJECT_ID(N'[dbo].[AzureKeyFieldsVersioned]', 'U') IS NOT NULL
    DROP TABLE [dbo].[AzureKeyFieldsVersioned];
GO
IF OBJECT_ID(N'[dbo].[Company]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Company];
GO
IF OBJECT_ID(N'[dbo].[EntityId]', 'U') IS NOT NULL
    DROP TABLE [dbo].[EntityId];
GO
IF OBJECT_ID(N'[dbo].[S3KeyFields]', 'U') IS NOT NULL
    DROP TABLE [dbo].[S3KeyFields];
GO
IF OBJECT_ID(N'[dbo].[StorageAccount]', 'U') IS NOT NULL
    DROP TABLE [dbo].[StorageAccount];
GO
IF OBJECT_ID(N'[dbo].[XmlKeyFields]', 'U') IS NOT NULL
    DROP TABLE [dbo].[XmlKeyFields];
GO
IF OBJECT_ID(N'[dbo].[UserAccess]', 'U') IS NOT NULL
    DROP TABLE [dbo].[UserAccess];
GO
