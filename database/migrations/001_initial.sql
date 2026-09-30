SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.SchemaMigrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SchemaMigrations
    (
        MigrationId nvarchar(150) NOT NULL CONSTRAINT PK_SchemaMigrations PRIMARY KEY,
        AppliedAt datetimeoffset NOT NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE MigrationId = N'001_initial')
BEGIN
    CREATE TABLE dbo.VisitorRequests
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_VisitorRequests PRIMARY KEY,
        RequestNumber nvarchar(32) NOT NULL,
        RequesterObjectId nvarchar(64) NOT NULL,
        MainHostObjectId nvarchar(64) NOT NULL,
        MainHostName nvarchar(200) NOT NULL,
        HostDepartment nvarchar(200) NOT NULL,
        EscortingHostObjectId nvarchar(64) NOT NULL,
        EscortingHostName nvarchar(200) NOT NULL,
        VisitorType nvarchar(20) NOT NULL,
        ContractorType nvarchar(40) NOT NULL,
        SiteCode nvarchar(50) NOT NULL,
        PurposeType nvarchar(30) NOT NULL,
        Purpose nvarchar(2000) NOT NULL,
        AreasToVisit nvarchar(1000) NOT NULL,
        Status nvarchar(40) NOT NULL,
        VisitStart datetimeoffset NOT NULL,
        VisitEnd datetimeoffset NOT NULL,
        CancellationReason nvarchar(2000) NOT NULL,
        CreatedAt datetimeoffset NOT NULL,
        UpdatedAt datetimeoffset NOT NULL,
        SubmittedAt datetimeoffset NULL,
        ApprovedAt datetimeoffset NULL,
        RejectedAt datetimeoffset NULL,
        RetainUntil datetimeoffset NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT CK_VisitorRequests_VisitWindow CHECK (VisitEnd > VisitStart)
    );
    CREATE UNIQUE INDEX UX_VisitorRequests_RequestNumber ON dbo.VisitorRequests(RequestNumber);
    CREATE INDEX IX_VisitorRequests_Requester_Updated ON dbo.VisitorRequests(RequesterObjectId, UpdatedAt);
    CREATE INDEX IX_VisitorRequests_Status_Start ON dbo.VisitorRequests(Status, VisitStart);
    CREATE INDEX IX_VisitorRequests_RetainUntil ON dbo.VisitorRequests(RetainUntil) WHERE RetainUntil IS NOT NULL;

    CREATE TABLE dbo.Visitors
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Visitors PRIMARY KEY,
        RequestId uniqueidentifier NOT NULL,
        Sequence int NOT NULL,
        DetailsStatus nvarchar(30) NOT NULL,
        FirstName nvarchar(100) NOT NULL,
        MiddleName nvarchar(100) NOT NULL,
        LastName nvarchar(100) NOT NULL,
        FullName nvarchar(320) NOT NULL,
        Citizenship nvarchar(100) NOT NULL,
        Designation nvarchar(200) NOT NULL,
        CompanyName nvarchar(250) NOT NULL,
        CompanyAddress nvarchar(1000) NOT NULL,
        OfficeCity nvarchar(120) NOT NULL,
        OfficeCountry nvarchar(120) NOT NULL,
        PhoneCountry nvarchar(120) NOT NULL,
        PhoneDialCode nvarchar(10) NOT NULL,
        Telephone nvarchar(30) NOT NULL,
        Email nvarchar(320) NOT NULL,
        IdType nvarchar(120) NOT NULL,
        OtherIdType nvarchar(200) NOT NULL,
        ScreeningDecision nvarchar(30) NOT NULL,
        Classification nvarchar(30) NOT NULL,
        ScreeningReason nvarchar(2000) NOT NULL,
        ScreeningDecidedAt datetimeoffset NULL,
        CONSTRAINT FK_Visitors_VisitorRequests FOREIGN KEY (RequestId) REFERENCES dbo.VisitorRequests(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX UX_Visitors_Request_Sequence ON dbo.Visitors(RequestId, Sequence);

    CREATE TABLE dbo.VisitDays
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_VisitDays PRIMARY KEY,
        RequestId uniqueidentifier NOT NULL,
        Date date NOT NULL,
        ExpectedArrival time NOT NULL,
        ExpectedDeparture time NOT NULL,
        CONSTRAINT FK_VisitDays_VisitorRequests FOREIGN KEY (RequestId) REFERENCES dbo.VisitorRequests(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX UX_VisitDays_Request_Date ON dbo.VisitDays(RequestId, Date);

    CREATE TABLE dbo.DeclaredAssets
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_DeclaredAssets PRIMARY KEY,
        VisitorId uniqueidentifier NOT NULL,
        AssetType nvarchar(100) NOT NULL,
        Description nvarchar(500) NOT NULL,
        SerialNumber nvarchar(200) NOT NULL,
        VerificationStatus nvarchar(30) NOT NULL,
        CONSTRAINT FK_DeclaredAssets_Visitors FOREIGN KEY (VisitorId) REFERENCES dbo.Visitors(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX UX_DeclaredAssets_Visitor_Serial ON dbo.DeclaredAssets(VisitorId, SerialNumber);

    CREATE TABLE dbo.DpsDocuments
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_DpsDocuments PRIMARY KEY,
        VisitorId uniqueidentifier NOT NULL,
        BlobName nvarchar(500) NOT NULL,
        FileName nvarchar(255) NOT NULL,
        Size bigint NOT NULL,
        Sha256 nvarchar(64) NOT NULL,
        UploadedBy nvarchar(64) NOT NULL,
        UploadedAt datetimeoffset NOT NULL,
        Version int NOT NULL,
        DeletedAt datetimeoffset NULL,
        CONSTRAINT FK_DpsDocuments_Visitors FOREIGN KEY (VisitorId) REFERENCES dbo.Visitors(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX UX_DpsDocuments_BlobName ON dbo.DpsDocuments(BlobName);
    CREATE UNIQUE INDEX UX_DpsDocuments_Visitor_Version ON dbo.DpsDocuments(VisitorId, Version);

    CREATE TABLE dbo.ReceptionRecords
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_ReceptionRecords PRIMARY KEY,
        VisitorId uniqueidentifier NOT NULL,
        VisitDayId uniqueidentifier NOT NULL,
        Status nvarchar(30) NOT NULL,
        IdentityStatus nvarchar(30) NOT NULL,
        AssetsStatus nvarchar(30) NOT NULL,
        ArrivalStatus nvarchar(30) NOT NULL,
        BadgeId nvarchar(100) NOT NULL,
        BadgeType nvarchar(100) NOT NULL,
        VerificationRemarks nvarchar(2000) NOT NULL,
        ActualArrival datetimeoffset NULL,
        ActualDeparture datetimeoffset NULL,
        BadgeReturnedAt datetimeoffset NULL,
        CONSTRAINT FK_ReceptionRecords_Visitors FOREIGN KEY (VisitorId) REFERENCES dbo.Visitors(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ReceptionRecords_VisitDays FOREIGN KEY (VisitDayId) REFERENCES dbo.VisitDays(Id)
    );
    CREATE UNIQUE INDEX UX_ReceptionRecords_Visitor_Day ON dbo.ReceptionRecords(VisitorId, VisitDayId);
    CREATE UNIQUE INDEX UX_ReceptionRecords_ActiveBadge ON dbo.ReceptionRecords(BadgeId) WHERE Status = N'CheckedIn' AND BadgeId <> N'';

    CREATE TABLE dbo.VisitorVersions
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_VisitorVersions PRIMARY KEY,
        VisitorId uniqueidentifier NOT NULL,
        Version int NOT NULL,
        SnapshotJson nvarchar(max) NOT NULL,
        CreatedBy nvarchar(64) NOT NULL,
        CreatedAt datetimeoffset NOT NULL,
        CONSTRAINT FK_VisitorVersions_Visitors FOREIGN KEY (VisitorId) REFERENCES dbo.Visitors(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX UX_VisitorVersions_Visitor_Version ON dbo.VisitorVersions(VisitorId, Version);

    CREATE TABLE dbo.AuditEvents
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_AuditEvents PRIMARY KEY,
        RequestId uniqueidentifier NOT NULL,
        Action nvarchar(100) NOT NULL,
        ActorId nvarchar(64) NOT NULL,
        ActorRole nvarchar(40) NOT NULL,
        BeforeJson nvarchar(max) NOT NULL,
        AfterJson nvarchar(max) NOT NULL,
        Details nvarchar(4000) NOT NULL,
        CorrelationId nvarchar(100) NOT NULL,
        OccurredAt datetimeoffset NOT NULL,
        CONSTRAINT FK_AuditEvents_VisitorRequests FOREIGN KEY (RequestId) REFERENCES dbo.VisitorRequests(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AuditEvents_Request_Occurred ON dbo.AuditEvents(RequestId, OccurredAt);

    CREATE TABLE dbo.InformationRequests
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_InformationRequests PRIMARY KEY,
        RequestId uniqueidentifier NOT NULL,
        VisitorId uniqueidentifier NOT NULL,
        FieldsJson nvarchar(max) NOT NULL,
        Instructions nvarchar(2000) NOT NULL,
        OriginalValuesJson nvarchar(max) NOT NULL,
        ChangesJson nvarchar(max) NOT NULL,
        CreatedBy nvarchar(64) NOT NULL,
        CreatedAt datetimeoffset NOT NULL,
        Status nvarchar(30) NOT NULL,
        ResolvedAt datetimeoffset NULL,
        CONSTRAINT FK_InformationRequests_VisitorRequests FOREIGN KEY (RequestId) REFERENCES dbo.VisitorRequests(Id) ON DELETE CASCADE,
        CONSTRAINT FK_InformationRequests_Visitors FOREIGN KEY (VisitorId) REFERENCES dbo.Visitors(Id)
    );

    CREATE TABLE dbo.ScheduleChanges
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_ScheduleChanges PRIMARY KEY,
        RequestId uniqueidentifier NOT NULL,
        PreviousStart datetimeoffset NOT NULL,
        PreviousEnd datetimeoffset NOT NULL,
        NewStart datetimeoffset NOT NULL,
        NewEnd datetimeoffset NOT NULL,
        Reason nvarchar(2000) NOT NULL,
        ChangedBy nvarchar(64) NOT NULL,
        ChangedAt datetimeoffset NOT NULL,
        CONSTRAINT FK_ScheduleChanges_VisitorRequests FOREIGN KEY (RequestId) REFERENCES dbo.VisitorRequests(Id) ON DELETE CASCADE
    );

    CREATE TABLE dbo.ScreeningReviews
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_ScreeningReviews PRIMARY KEY,
        RequestId uniqueidentifier NOT NULL,
        VisitorId uniqueidentifier NOT NULL,
        Decision nvarchar(30) NOT NULL,
        Classification nvarchar(30) NOT NULL,
        Comments nvarchar(2000) NOT NULL,
        ReviewerId nvarchar(64) NOT NULL,
        ReviewedAt datetimeoffset NOT NULL,
        CONSTRAINT FK_ScreeningReviews_VisitorRequests FOREIGN KEY (RequestId) REFERENCES dbo.VisitorRequests(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ScreeningReviews_Visitors FOREIGN KEY (VisitorId) REFERENCES dbo.Visitors(Id)
    );

    CREATE TABLE dbo.DocumentAccessEvents
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_DocumentAccessEvents PRIMARY KEY,
        DocumentId uniqueidentifier NOT NULL,
        RequestId uniqueidentifier NOT NULL,
        VisitorId uniqueidentifier NOT NULL,
        ActorId nvarchar(64) NOT NULL,
        Action nvarchar(50) NOT NULL,
        CorrelationId nvarchar(100) NOT NULL,
        OccurredAt datetimeoffset NOT NULL,
        CONSTRAINT FK_DocumentAccessEvents_VisitorRequests FOREIGN KEY (RequestId) REFERENCES dbo.VisitorRequests(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_DocumentAccessEvents_Document_Occurred ON dbo.DocumentAccessEvents(DocumentId, OccurredAt);

    CREATE TABLE dbo.ReportExportEvents
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_ReportExportEvents PRIMARY KEY,
        ActorId nvarchar(64) NOT NULL,
        ActorRole nvarchar(40) NOT NULL,
        Format nvarchar(20) NOT NULL,
        FiltersJson nvarchar(max) NOT NULL,
        RowCount int NOT NULL,
        CorrelationId nvarchar(100) NOT NULL,
        ExportedAt datetimeoffset NOT NULL
    );
    CREATE INDEX IX_ReportExportEvents_ExportedAt ON dbo.ReportExportEvents(ExportedAt);

    CREATE TABLE dbo.OutboxMessages
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_OutboxMessages PRIMARY KEY,
        MessageType nvarchar(200) NOT NULL,
        PayloadJson nvarchar(max) NOT NULL,
        OccurredAt datetimeoffset NOT NULL,
        ProcessedAt datetimeoffset NULL,
        AttemptCount int NOT NULL,
        LastError nvarchar(4000) NOT NULL
    );
    CREATE INDEX IX_OutboxMessages_Processed_Occurred ON dbo.OutboxMessages(ProcessedAt, OccurredAt);

    CREATE TABLE dbo.ReferenceOptions
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_ReferenceOptions PRIMARY KEY,
        Category nvarchar(100) NOT NULL,
        Code nvarchar(100) NOT NULL,
        DisplayName nvarchar(200) NOT NULL,
        SortOrder int NOT NULL,
        IsActive bit NOT NULL
    );
    CREATE UNIQUE INDEX UX_ReferenceOptions_Category_Code ON dbo.ReferenceOptions(Category, Code);

    CREATE TABLE dbo.RequestSequences
    (
        Year int NOT NULL,
        Month int NOT NULL,
        CurrentValue int NOT NULL,
        CONSTRAINT PK_RequestSequences PRIMARY KEY (Year, Month),
        CONSTRAINT CK_RequestSequences_Month CHECK (Month BETWEEN 1 AND 12)
    );

    INSERT dbo.ReferenceOptions(Id, Category, Code, DisplayName, SortOrder, IsActive)
    VALUES
        (NEWID(), N'Site', N'BLR', N'Bengaluru', 10, 1),
        (NEWID(), N'Site', N'DEL', N'Delhi', 20, 1),
        (NEWID(), N'IdentityDocument', N'PASSPORT', N'Passport', 10, 1),
        (NEWID(), N'IdentityDocument', N'NATIONAL_ID', N'National ID', 20, 1),
        (NEWID(), N'IdentityDocument', N'OTHER_GOV', N'Other Government Issued ID', 30, 1);

    INSERT dbo.SchemaMigrations(MigrationId, AppliedAt) VALUES (N'001_initial', SYSDATETIMEOFFSET());
END;

COMMIT TRANSACTION;
