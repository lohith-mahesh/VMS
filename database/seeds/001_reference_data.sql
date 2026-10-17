SET XACT_ABORT ON;
BEGIN TRANSACTION;

MERGE dbo.ReferenceOptions AS target
USING (VALUES
    (N'Site', N'BLR', N'Bengaluru', 10),
    (N'Site', N'DEL', N'Delhi', 20),
    (N'IdentityDocument', N'DRIVER_LICENSE', N'Driver''s License', 5),
    (N'IdentityDocument', N'PASSPORT', N'Passport', 10),
    (N'IdentityDocument', N'VOTER_ID', N'Voter ID', 15),
    (N'IdentityDocument', N'AADHAAR', N'Aadhaar Card', 20),
    (N'IdentityDocument', N'PAN', N'PAN Card', 25),
    (N'IdentityDocument', N'OTHER_GOV', N'Other Government Issued ID', 30)
) AS source(Category, Code, DisplayName, SortOrder)
ON target.Category = source.Category AND target.Code = source.Code
WHEN MATCHED THEN UPDATE SET DisplayName = source.DisplayName, SortOrder = source.SortOrder, IsActive = 1
WHEN NOT MATCHED THEN INSERT(Id, Category, Code, DisplayName, SortOrder, IsActive) VALUES(NEWID(), source.Category, source.Code, source.DisplayName, source.SortOrder, 1);

UPDATE dbo.ReferenceOptions SET IsActive = 0 WHERE Category = N'IdentityDocument' AND Code = N'NATIONAL_ID';

COMMIT TRANSACTION;
