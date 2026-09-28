-- ─────────────────────────────────────────────────────────────────────────────
-- Migration Script: Ensure_PoliceRecords_Defaults.sql
-- Fixes HTTP 500 error when issuing character certificates by ensuring NOT NULL
-- boolean/datetime columns have proper DEFAULT constraints in SQL Server.
-- ─────────────────────────────────────────────────────────────────────────────

USE [Fyp-1];
GO

-- 1. Default constraint for IsRevoked (0 = active/not revoked)
IF NOT EXISTS (SELECT * FROM sys.default_constraints WHERE name = 'DF_PoliceRecords_IsRevoked')
BEGIN
    ALTER TABLE dbo.PoliceRecords ADD CONSTRAINT DF_PoliceRecords_IsRevoked DEFAULT 0 FOR IsRevoked;
    PRINT 'Added DF_PoliceRecords_IsRevoked default constraint.';
END
GO

-- 2. Default constraint for CnicVerified (0 = false)
IF NOT EXISTS (SELECT * FROM sys.default_constraints WHERE name = 'DF_PoliceRecords_CnicVerified')
BEGIN
    ALTER TABLE dbo.PoliceRecords ADD CONSTRAINT DF_PoliceRecords_CnicVerified DEFAULT 0 FOR CnicVerified;
    PRINT 'Added DF_PoliceRecords_CnicVerified default constraint.';
END
GO

-- 3. Default constraint for IssuedDate (GETDATE())
IF NOT EXISTS (SELECT * FROM sys.default_constraints WHERE name = 'DF_PoliceRecords_IssuedDate')
BEGIN
    ALTER TABLE dbo.PoliceRecords ADD CONSTRAINT DF_PoliceRecords_IssuedDate DEFAULT GETDATE() FOR IssuedDate;
    PRINT 'Added DF_PoliceRecords_IssuedDate default constraint.';
END
GO

PRINT 'Ensure_PoliceRecords_Defaults script executed successfully.';
