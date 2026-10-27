/*
  TreeRecycle: Ireland-only -> multi-country (IE, GB, AU, NZ, US).
  Run once against any database created with the ORIGINAL 01_create_tables.sql. Safe to re-run: every step
  checks first. A database created with the CURRENT 01_create_tables.sql already has all of this.

  Club and order tables:  county -> region (NVARCHAR(100)),  eircode -> postcode (VARCHAR(12)),  + country_code
  Club table only:        + time_zone
  Existing rows are all Irish, so country_code defaults to 'IE' and time_zone to 'GMT Standard Time'.
*/
SET NOCOUNT ON;
GO

/* ---- TreeRecycle_club_tbl ---- */
IF COL_LENGTH('dbo.TreeRecycle_club_tbl', 'county') IS NOT NULL AND COL_LENGTH('dbo.TreeRecycle_club_tbl', 'region') IS NULL
BEGIN
    IF INDEXPROPERTY(OBJECT_ID('dbo.TreeRecycle_club_tbl'), 'IX_TreeRecycle_club_tbl_StatusCounty', 'IndexID') IS NOT NULL
        DROP INDEX IX_TreeRecycle_club_tbl_StatusCounty ON dbo.TreeRecycle_club_tbl;
    EXEC sp_rename 'dbo.TreeRecycle_club_tbl.county', 'region', 'COLUMN';
    EXEC sp_rename 'DF_TreeRecycle_club_tbl_County', 'DF_TreeRecycle_club_tbl_Region', 'OBJECT';
END
GO
ALTER TABLE dbo.TreeRecycle_club_tbl ALTER COLUMN region NVARCHAR(100) NOT NULL;
GO
IF COL_LENGTH('dbo.TreeRecycle_club_tbl', 'eircode') IS NOT NULL AND COL_LENGTH('dbo.TreeRecycle_club_tbl', 'postcode') IS NULL
BEGIN
    EXEC sp_rename 'dbo.TreeRecycle_club_tbl.eircode', 'postcode', 'COLUMN';
    EXEC sp_rename 'DF_TreeRecycle_club_tbl_Eircode', 'DF_TreeRecycle_club_tbl_Postcode', 'OBJECT';
END
GO
ALTER TABLE dbo.TreeRecycle_club_tbl ALTER COLUMN postcode VARCHAR(12) NOT NULL;
GO
IF COL_LENGTH('dbo.TreeRecycle_club_tbl', 'country_code') IS NULL
    ALTER TABLE dbo.TreeRecycle_club_tbl ADD country_code VARCHAR(2) NOT NULL CONSTRAINT DF_TreeRecycle_club_tbl_CountryCode DEFAULT ('IE');
GO
IF COL_LENGTH('dbo.TreeRecycle_club_tbl', 'time_zone') IS NULL
    ALTER TABLE dbo.TreeRecycle_club_tbl ADD time_zone VARCHAR(50) NOT NULL CONSTRAINT DF_TreeRecycle_club_tbl_TimeZone DEFAULT ('GMT Standard Time');
GO
IF INDEXPROPERTY(OBJECT_ID('dbo.TreeRecycle_club_tbl'), 'IX_TreeRecycle_club_tbl_StatusCountryRegion', 'IndexID') IS NULL
    CREATE NONCLUSTERED INDEX IX_TreeRecycle_club_tbl_StatusCountryRegion ON dbo.TreeRecycle_club_tbl (status, country_code, region);
GO

/* ---- TreeRecycle_tree_order_tbl ---- */
IF COL_LENGTH('dbo.TreeRecycle_tree_order_tbl', 'county') IS NOT NULL AND COL_LENGTH('dbo.TreeRecycle_tree_order_tbl', 'region') IS NULL
BEGIN
    EXEC sp_rename 'dbo.TreeRecycle_tree_order_tbl.county', 'region', 'COLUMN';
    EXEC sp_rename 'DF_TreeRecycle_tree_order_tbl_County', 'DF_TreeRecycle_tree_order_tbl_Region', 'OBJECT';
END
GO
ALTER TABLE dbo.TreeRecycle_tree_order_tbl ALTER COLUMN region NVARCHAR(100) NOT NULL;
GO
IF COL_LENGTH('dbo.TreeRecycle_tree_order_tbl', 'eircode') IS NOT NULL AND COL_LENGTH('dbo.TreeRecycle_tree_order_tbl', 'postcode') IS NULL
BEGIN
    EXEC sp_rename 'dbo.TreeRecycle_tree_order_tbl.eircode', 'postcode', 'COLUMN';
    EXEC sp_rename 'DF_TreeRecycle_tree_order_tbl_Eircode', 'DF_TreeRecycle_tree_order_tbl_Postcode', 'OBJECT';
END
GO
ALTER TABLE dbo.TreeRecycle_tree_order_tbl ALTER COLUMN postcode VARCHAR(12) NOT NULL;
GO
IF COL_LENGTH('dbo.TreeRecycle_tree_order_tbl', 'country_code') IS NULL
    ALTER TABLE dbo.TreeRecycle_tree_order_tbl ADD country_code VARCHAR(2) NOT NULL CONSTRAINT DF_TreeRecycle_tree_order_tbl_CountryCode DEFAULT ('IE');
GO

PRINT 'TreeRecycle international upgrade complete.';
