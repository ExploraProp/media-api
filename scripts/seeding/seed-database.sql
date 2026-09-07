-- Media seed-database.sql
-- Script to seed MediaPolicy data
-- MediaPolicy is a read-only lookup entity keyed by OwnerType.
-- Adding or changing upload limits requires updating this script.
-- Depends on: MediaPolicy table (AddMediaTables migration)

SET NOCOUNT ON;
GO

DECLARE @CurrentTime DATETIME2 = GETUTCDATE();

-- ===================================================
-- Media policies (one row per OwnerType)
-- OwnerType: 0=User, 1=Listing, 2=Identification, 3=VerificationSubmission
-- AccessLevel: 0=Public, 1=Private
-- AllowedContentTypes (flags): Jpeg=1, Png=2, Gif=4, WebP=8, Pdf=16
-- ===================================================
PRINT 'Creating MediaPolicy rows...';
PRINT '---------------------------------------------------';

-- User — profile photos (public, 8 MB, max 10)
IF NOT EXISTS (SELECT 1 FROM MediaPolicy WHERE OwnerType = 0)
BEGIN
    INSERT INTO MediaPolicy (Id, OwnerType, MaxFileSizeBytes, AllowedContentTypes, MaxMediaCount, AccessLevel, WatermarkEnabled, CreatedAt, UpdatedAt)
    VALUES ('11111111-1111-1111-1111-111111111101', 0, 8388608, 15, 10, 0, 0, @CurrentTime, @CurrentTime);
    PRINT 'MediaPolicy User created.';
END
ELSE
    PRINT 'MediaPolicy User already exists.';

-- Listing — property photos (public, watermarked, 16 MB, max 50)
IF NOT EXISTS (SELECT 1 FROM MediaPolicy WHERE OwnerType = 1)
BEGIN
    INSERT INTO MediaPolicy (Id, OwnerType, MaxFileSizeBytes, AllowedContentTypes, MaxMediaCount, AccessLevel, WatermarkEnabled, CreatedAt, UpdatedAt)
    VALUES ('11111111-1111-1111-1111-111111111102', 1, 16777216, 15, 50, 0, 1, @CurrentTime, @CurrentTime);
    PRINT 'MediaPolicy Listing created.';
END
ELSE
    PRINT 'MediaPolicy Listing already exists.';

-- Identification — KYC documents (private, 4 MB, max 5)
IF NOT EXISTS (SELECT 1 FROM MediaPolicy WHERE OwnerType = 2)
BEGIN
    INSERT INTO MediaPolicy (Id, OwnerType, MaxFileSizeBytes, AllowedContentTypes, MaxMediaCount, AccessLevel, WatermarkEnabled, CreatedAt, UpdatedAt)
    VALUES ('11111111-1111-1111-1111-111111111103', 2, 4194304, 19, 5, 1, 0, @CurrentTime, @CurrentTime);
    PRINT 'MediaPolicy Identification created.';
END
ELSE
    PRINT 'MediaPolicy Identification already exists.';

-- VerificationSubmission — reviewer uploads (private, 4 MB, max 5)
IF NOT EXISTS (SELECT 1 FROM MediaPolicy WHERE OwnerType = 3)
BEGIN
    INSERT INTO MediaPolicy (Id, OwnerType, MaxFileSizeBytes, AllowedContentTypes, MaxMediaCount, AccessLevel, WatermarkEnabled, CreatedAt, UpdatedAt)
    VALUES ('11111111-1111-1111-1111-111111111104', 3, 4194304, 19, 5, 1, 0, @CurrentTime, @CurrentTime);
    PRINT 'MediaPolicy VerificationSubmission created.';
END
ELSE
    PRINT 'MediaPolicy VerificationSubmission already exists.';

PRINT '';
PRINT '---------------------------------------------------';
PRINT 'MediaPolicy seeding completed successfully!';
GO
