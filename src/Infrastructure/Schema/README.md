# Media database schema

SQL Server schema for the Media bounded context (`MediaDbContext`). Source of truth: EF Core configurations under `../Configurations/` and migrations under `../Migrations/`. Index name constants live beside this file.

## Entity-relationship diagram

```mermaid
erDiagram
    MediaFile {
        uniqueidentifier Id PK
        uniqueidentifier OwnerId
        int OwnerType
        nvarchar MediaType
        nvarchar ContentType
        int AccessLevel
        nvarchar Status
        nvarchar QuarantinePath "nullable"
        nvarchar PublicUrl "nullable"
        bigint FileSizeBytes
        int ScanAttempts
        datetime2 UploadedAt
        datetime2 ApprovedAt "nullable"
        uniqueidentifier UploadedByClientUserId "nullable"
        datetime2 CreatedAt
        datetime2 UpdatedAt
        datetime2 DeletedAt "nullable, soft delete"
    }

    MediaPolicy {
        uniqueidentifier Id PK
        int OwnerType
        bigint MaxFileSizeBytes
        int AllowedContentTypes
        int MaxMediaCount
        int AccessLevel
        bit WatermarkEnabled
        datetime2 CreatedAt
        datetime2 UpdatedAt
        datetime2 DeletedAt "nullable, soft delete"
    }

    MediaVariant {
        uniqueidentifier Id PK
        uniqueidentifier MediaFileId FK
        int VariantType
        nvarchar Url
        int Width "nullable"
        int Height "nullable"
        bigint FileSizeBytes "nullable"
        datetime2 CreatedAt
        datetime2 UpdatedAt
        datetime2 DeletedAt "nullable, soft delete"
    }

    OutboxEvent {
        uniqueidentifier Id PK
        uniqueidentifier AggregateId
        nvarchar AggregateType
        nvarchar EventType
        nvarchar EventData
        bit IsPublished
        datetime2 PublishedAt "nullable"
        int RetryCount
        datetime2 CreatedAt
        datetime2 UpdatedAt
        datetime2 DeletedAt "nullable"
    }

    MediaFile ||--o{ MediaVariant : "MediaFileId"
}
```

## Tables

| Table | Role |
|-------|------|
| `MediaFile` | Uploaded file aggregate including moderation and publishing state |
| `MediaPolicy` | Upload rules per owner type |
| `MediaVariant` | Derived variants such as thumbnails or transformed assets |
| `OutboxEvent` | SharedKernel transactional outbox |

### Relationships

- **`MediaVariant.MediaFileId` -> `MediaFile.Id`** - required FK with cascade delete.
- **`OwnerId`** is a logical polymorphic reference to the owning entity in another bounded context.

### Soft delete

`MediaFile`, `MediaPolicy`, `MediaVariant`, and `OutboxEvent` use `DeletedAt` soft-delete timestamps.

## Indexes

| Index | Columns | Notes |
|-------|---------|-------|
| `IX_MediaFile_OwnerId` | `OwnerId` | Owner lookup |
| `IX_MediaFile_OwnerId_OwnerType` | (`OwnerId`, `OwnerType`) | Polymorphic owner lookup |
| `IX_MediaFile_Status` | `Status` | Scan/approval workflow |
| `IX_MediaPolicy_OwnerType` | `OwnerType` UNIQUE | One policy per owner type |
| `IX_MediaVariant_MediaFileId` | `MediaFileId` | Variant traversal |
| `UX_MediaVariant_MediaFileId_VariantType` | (`MediaFileId`, `VariantType`) UNIQUE | One variant per type |
| `IX_OutboxEvent_AggregateId` | `AggregateId` | Outbox lookup |
| `IX_OutboxEvent_IsPublished` | `IsPublished` | Publish workers |
| `IX_OutboxEvent_Unpublished` | (`IsPublished`, `RetryCount`, `CreatedAt`) | Unpublished queue |

All index names are centralized in `MediaConstraints.cs`.

## Migrations & seed

```powershell
# From repo root
dotnet tool run api-scripts -- seed --service .
```

Seed data (`scripts/seeding/seed-database.sql`) loads default `MediaPolicy` rows by owner type.
