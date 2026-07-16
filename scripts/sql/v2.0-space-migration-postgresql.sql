-- PrivateCloudDrive V2.0 Space 数据底座迁移草案（PostgreSQL）
-- ADR: docs/adr/adr-04-v2-space-db-migration-rollback.md
-- 适用：测试库 dry-run 与 EF Core Migration.Sql(...) 拆分落地前的 DBA 审查脚本
-- 重要：生产执行前必须完成 pg_dump、storage volume snapshot、.env/.secrets 备份。

\echo 'V2.0 Space migration: start'

-- -----------------------------------------------------------------------------
-- Step 0. 前置统计：执行方应保存本段输出作为 before baseline。
-- -----------------------------------------------------------------------------
SELECT 'before.AppFileCenterFileNodes' AS metric, count(*) AS value FROM "AppFileCenterFileNodes"
UNION ALL SELECT 'before.AppFileCenterBlobObjects', count(*) FROM "AppFileCenterBlobObjects"
UNION ALL SELECT 'before.AppFileCenterUploadSessions', count(*) FROM "AppFileCenterUploadSessions"
UNION ALL SELECT 'before.AppFileCenterMediaAssets', count(*) FROM "AppFileCenterMediaAssets"
UNION ALL SELECT 'before.AppFileCenterMediaAlbums', count(*) FROM "AppFileCenterMediaAlbums"
UNION ALL SELECT 'before.AppFileCenterMediaAlbumItems', count(*) FROM "AppFileCenterMediaAlbumItems"
UNION ALL SELECT 'before.AppFileCenterFileShares', count(*) FROM "AppFileCenterFileShares"
UNION ALL SELECT 'before.AppFileCenterFileTags', count(*) FROM "AppFileCenterFileTags"
UNION ALL SELECT 'before.AppFileCenterFileNodeTags', count(*) FROM "AppFileCenterFileNodeTags"
UNION ALL SELECT 'before.AppFileCenterOperationLogs', count(*) FROM "AppFileCenterOperationLogs";

-- -----------------------------------------------------------------------------
-- Step 1. 新增 Space / SpaceMember / SpacePermission 表。
-- SpaceType: 0=Personal, 1=Family, 2=Team
-- Role: 0=Owner, 1=Admin, 2=Member, 3=Viewer
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "AppSpaces" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NULL,
    "OwnerUserId" uuid NOT NULL,
    "Name" character varying(128) NOT NULL,
    "NormalizedName" character varying(128) NOT NULL,
    "Description" character varying(512) NULL,
    "SpaceType" integer NOT NULL,
    "IsDefaultPersonal" boolean NOT NULL DEFAULT false,
    "QuotaBytes" bigint NULL,
    "UsedBytesSnapshot" bigint NOT NULL DEFAULT 0,
    "Status" integer NOT NULL DEFAULT 0,
    "ExtraProperties" text NOT NULL DEFAULT '{}',
    "ConcurrencyStamp" character varying(40) NOT NULL DEFAULT '',
    "CreationTime" timestamp without time zone NOT NULL,
    "CreatorId" uuid NULL,
    "LastModificationTime" timestamp without time zone NULL,
    "LastModifierId" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeleterId" uuid NULL,
    "DeletionTime" timestamp without time zone NULL,
    CONSTRAINT "PK_AppSpaces" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "AppSpaceMembers" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NULL,
    "SpaceId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Role" integer NOT NULL,
    "IsDisabled" boolean NOT NULL DEFAULT false,
    "JoinedTime" timestamp without time zone NOT NULL,
    "ExtraProperties" text NOT NULL DEFAULT '{}',
    "ConcurrencyStamp" character varying(40) NOT NULL DEFAULT '',
    "CreationTime" timestamp without time zone NOT NULL,
    "CreatorId" uuid NULL,
    "LastModificationTime" timestamp without time zone NULL,
    "LastModifierId" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeleterId" uuid NULL,
    "DeletionTime" timestamp without time zone NULL,
    CONSTRAINT "PK_AppSpaceMembers" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "AppSpacePermissions" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NULL,
    "SpaceId" uuid NOT NULL,
    "Role" integer NOT NULL,
    "PermissionName" character varying(128) NOT NULL,
    "IsGranted" boolean NOT NULL DEFAULT true,
    "CreationTime" timestamp without time zone NOT NULL,
    "CreatorId" uuid NULL,
    CONSTRAINT "PK_AppSpacePermissions" PRIMARY KEY ("Id")
);

-- -----------------------------------------------------------------------------
-- Step 2. 给现有表增加 nullable SpaceId / 审计角色字段。
-- -----------------------------------------------------------------------------
ALTER TABLE "AppFileCenterFileNodes" ADD COLUMN IF NOT EXISTS "SpaceId" uuid NULL;
ALTER TABLE "AppFileCenterBlobObjects" ADD COLUMN IF NOT EXISTS "SpaceId" uuid NULL;
ALTER TABLE "AppFileCenterUploadSessions" ADD COLUMN IF NOT EXISTS "SpaceId" uuid NULL;
ALTER TABLE "AppFileCenterMediaAssets" ADD COLUMN IF NOT EXISTS "SpaceId" uuid NULL;
ALTER TABLE "AppFileCenterMediaAlbums" ADD COLUMN IF NOT EXISTS "SpaceId" uuid NULL;
ALTER TABLE "AppFileCenterMediaAlbumItems" ADD COLUMN IF NOT EXISTS "SpaceId" uuid NULL;
ALTER TABLE "AppFileCenterFileShares" ADD COLUMN IF NOT EXISTS "SpaceId" uuid NULL;
ALTER TABLE "AppFileCenterFileTags" ADD COLUMN IF NOT EXISTS "SpaceId" uuid NULL;
ALTER TABLE "AppFileCenterFileNodeTags" ADD COLUMN IF NOT EXISTS "SpaceId" uuid NULL;
ALTER TABLE "AppFileCenterOperationLogs" ADD COLUMN IF NOT EXISTS "SpaceId" uuid NULL;
ALTER TABLE "AppFileCenterOperationLogs" ADD COLUMN IF NOT EXISTS "OperatorSpaceRole" integer NULL;

-- -----------------------------------------------------------------------------
-- Step 3. 生成 owner -> 默认个人空间映射。
-- 使用确定性 UUID：md5(TenantId + OwnerId + purpose) 拼接为 UUID。
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS "_pcd_v2_space_owner_map";
CREATE TEMP TABLE "_pcd_v2_space_owner_map" AS
WITH owners AS (
    SELECT "TenantId", "OwnerId" FROM "AppFileCenterFileNodes"
    UNION SELECT "TenantId", "OwnerId" FROM "AppFileCenterBlobObjects"
    UNION SELECT "TenantId", "OwnerId" FROM "AppFileCenterUploadSessions"
    UNION SELECT "TenantId", "OwnerId" FROM "AppFileCenterMediaAssets"
    UNION SELECT "TenantId", "OwnerId" FROM "AppFileCenterMediaAlbums"
    UNION SELECT "TenantId", "OwnerId" FROM "AppFileCenterMediaAlbumItems"
    UNION SELECT "TenantId", "OwnerId" FROM "AppFileCenterFileShares"
    UNION SELECT "TenantId", "OwnerId" FROM "AppFileCenterFileTags"
    UNION SELECT "TenantId", "OwnerId" FROM "AppFileCenterFileNodeTags"
), normalized AS (
    SELECT DISTINCT "TenantId", "OwnerId"
    FROM owners
    WHERE "OwnerId" IS NOT NULL
), hashes AS (
    SELECT
        "TenantId",
        "OwnerId",
        md5(coalesce("TenantId"::text, 'host') || ':' || "OwnerId"::text || ':default-personal-space') AS h
    FROM normalized
)
SELECT
    "TenantId",
    "OwnerId",
    (substr(h, 1, 8) || '-' || substr(h, 9, 4) || '-' || substr(h, 13, 4) || '-' || substr(h, 17, 4) || '-' || substr(h, 21, 12))::uuid AS "SpaceId"
FROM hashes;

-- -----------------------------------------------------------------------------
-- Step 4. 插入默认个人空间和 Owner 成员。
-- -----------------------------------------------------------------------------
INSERT INTO "AppSpaces" (
    "Id", "TenantId", "OwnerUserId", "Name", "NormalizedName", "Description",
    "SpaceType", "IsDefaultPersonal", "QuotaBytes", "UsedBytesSnapshot", "Status",
    "ExtraProperties", "ConcurrencyStamp", "CreationTime", "CreatorId",
    "IsDeleted"
)
SELECT
    m."SpaceId",
    m."TenantId",
    m."OwnerId",
    '个人空间',
    'PERSONAL-' || upper(m."OwnerId"::text),
    'V2.0 migration generated default personal space',
    0,
    true,
    NULL,
    coalesce((
        SELECT sum(fn."Size") FROM "AppFileCenterFileNodes" fn
        WHERE fn."OwnerId" = m."OwnerId"
          AND fn."TenantId" IS NOT DISTINCT FROM m."TenantId"
          AND fn."IsDeleted" = false
    ), 0),
    0,
    '{}',
    substr(md5(random()::text || clock_timestamp()::text), 1, 40),
    now() AT TIME ZONE 'UTC',
    m."OwnerId",
    false
FROM "_pcd_v2_space_owner_map" m
ON CONFLICT ("Id") DO UPDATE SET
    "UsedBytesSnapshot" = EXCLUDED."UsedBytesSnapshot",
    "LastModificationTime" = now() AT TIME ZONE 'UTC';

INSERT INTO "AppSpaceMembers" (
    "Id", "TenantId", "SpaceId", "UserId", "Role", "IsDisabled", "JoinedTime",
    "ExtraProperties", "ConcurrencyStamp", "CreationTime", "CreatorId", "IsDeleted"
)
SELECT
    (
        substr(md5(coalesce(m."TenantId"::text, 'host') || ':' || m."SpaceId"::text || ':' || m."OwnerId"::text || ':owner-member'), 1, 8) || '-' ||
        substr(md5(coalesce(m."TenantId"::text, 'host') || ':' || m."SpaceId"::text || ':' || m."OwnerId"::text || ':owner-member'), 9, 4) || '-' ||
        substr(md5(coalesce(m."TenantId"::text, 'host') || ':' || m."SpaceId"::text || ':' || m."OwnerId"::text || ':owner-member'), 13, 4) || '-' ||
        substr(md5(coalesce(m."TenantId"::text, 'host') || ':' || m."SpaceId"::text || ':' || m."OwnerId"::text || ':owner-member'), 17, 4) || '-' ||
        substr(md5(coalesce(m."TenantId"::text, 'host') || ':' || m."SpaceId"::text || ':' || m."OwnerId"::text || ':owner-member'), 21, 12)
    )::uuid,
    m."TenantId",
    m."SpaceId",
    m."OwnerId",
    0,
    false,
    now() AT TIME ZONE 'UTC',
    '{}',
    substr(md5(random()::text || clock_timestamp()::text), 1, 40),
    now() AT TIME ZONE 'UTC',
    m."OwnerId",
    false
FROM "_pcd_v2_space_owner_map" m
ON CONFLICT ("Id") DO NOTHING;

-- 默认个人空间角色权限种子。MVP 固定权限也写表，方便审计与后续配置化。
WITH role_permissions(role_value, permission_name) AS (
    VALUES
      (0, 'Files.View'), (0, 'Files.Upload'), (0, 'Files.Edit'), (0, 'Files.Delete'), (0, 'Files.PermanentDelete'), (0, 'Members.Manage'), (0, 'Space.Configure'), (0, 'Quota.Manage'),
      (1, 'Files.View'), (1, 'Files.Upload'), (1, 'Files.Edit'), (1, 'Files.Delete'), (1, 'Files.PermanentDelete'), (1, 'Members.Manage'),
      (2, 'Files.View'), (2, 'Files.Upload'), (2, 'Files.Edit'),
      (3, 'Files.View')
), seed AS (
    SELECT
        s."TenantId",
        s."Id" AS "SpaceId",
        rp.role_value AS "Role",
        rp.permission_name AS "PermissionName",
        md5(coalesce(s."TenantId"::text, 'host') || ':' || s."Id"::text || ':' || rp.role_value::text || ':' || rp.permission_name) AS h
    FROM "AppSpaces" s
    CROSS JOIN role_permissions rp
    WHERE s."IsDefaultPersonal" = true
)
INSERT INTO "AppSpacePermissions" ("Id", "TenantId", "SpaceId", "Role", "PermissionName", "IsGranted", "CreationTime", "CreatorId")
SELECT
    (substr(h, 1, 8) || '-' || substr(h, 9, 4) || '-' || substr(h, 13, 4) || '-' || substr(h, 17, 4) || '-' || substr(h, 21, 12))::uuid,
    "TenantId",
    "SpaceId",
    "Role",
    "PermissionName",
    true,
    now() AT TIME ZONE 'UTC',
    NULL
FROM seed
ON CONFLICT ("Id") DO NOTHING;

-- -----------------------------------------------------------------------------
-- Step 5. 回填现有表 SpaceId。
-- -----------------------------------------------------------------------------
UPDATE "AppFileCenterFileNodes" t
SET "SpaceId" = m."SpaceId"
FROM "_pcd_v2_space_owner_map" m
WHERE t."SpaceId" IS NULL
  AND t."OwnerId" = m."OwnerId"
  AND t."TenantId" IS NOT DISTINCT FROM m."TenantId";

UPDATE "AppFileCenterBlobObjects" t
SET "SpaceId" = m."SpaceId"
FROM "_pcd_v2_space_owner_map" m
WHERE t."SpaceId" IS NULL
  AND t."OwnerId" = m."OwnerId"
  AND t."TenantId" IS NOT DISTINCT FROM m."TenantId";

UPDATE "AppFileCenterUploadSessions" t
SET "SpaceId" = m."SpaceId"
FROM "_pcd_v2_space_owner_map" m
WHERE t."SpaceId" IS NULL
  AND t."OwnerId" = m."OwnerId"
  AND t."TenantId" IS NOT DISTINCT FROM m."TenantId";

UPDATE "AppFileCenterMediaAssets" ma
SET "SpaceId" = fn."SpaceId"
FROM "AppFileCenterFileNodes" fn
WHERE ma."SpaceId" IS NULL
  AND ma."FileNodeId" = fn."Id"
  AND fn."SpaceId" IS NOT NULL;

UPDATE "AppFileCenterMediaAssets" t
SET "SpaceId" = m."SpaceId"
FROM "_pcd_v2_space_owner_map" m
WHERE t."SpaceId" IS NULL
  AND t."OwnerId" = m."OwnerId"
  AND t."TenantId" IS NOT DISTINCT FROM m."TenantId";

UPDATE "AppFileCenterMediaAlbums" t
SET "SpaceId" = m."SpaceId"
FROM "_pcd_v2_space_owner_map" m
WHERE t."SpaceId" IS NULL
  AND t."OwnerId" = m."OwnerId"
  AND t."TenantId" IS NOT DISTINCT FROM m."TenantId";

UPDATE "AppFileCenterMediaAlbumItems" item
SET "SpaceId" = coalesce(fn."SpaceId", album."SpaceId")
FROM "AppFileCenterMediaAlbums" album
LEFT JOIN "AppFileCenterFileNodes" fn ON fn."Id" = item."FileNodeId"
WHERE item."SpaceId" IS NULL
  AND item."AlbumId" = album."Id"
  AND coalesce(fn."SpaceId", album."SpaceId") IS NOT NULL;

UPDATE "AppFileCenterMediaAlbumItems" t
SET "SpaceId" = m."SpaceId"
FROM "_pcd_v2_space_owner_map" m
WHERE t."SpaceId" IS NULL
  AND t."OwnerId" = m."OwnerId"
  AND t."TenantId" IS NOT DISTINCT FROM m."TenantId";

UPDATE "AppFileCenterFileShares" share
SET "SpaceId" = fn."SpaceId"
FROM "AppFileCenterFileNodes" fn
WHERE share."SpaceId" IS NULL
  AND share."FileNodeId" = fn."Id"
  AND fn."SpaceId" IS NOT NULL;

UPDATE "AppFileCenterFileShares" t
SET "SpaceId" = m."SpaceId"
FROM "_pcd_v2_space_owner_map" m
WHERE t."SpaceId" IS NULL
  AND t."OwnerId" = m."OwnerId"
  AND t."TenantId" IS NOT DISTINCT FROM m."TenantId";

UPDATE "AppFileCenterFileTags" t
SET "SpaceId" = m."SpaceId"
FROM "_pcd_v2_space_owner_map" m
WHERE t."SpaceId" IS NULL
  AND t."OwnerId" = m."OwnerId"
  AND t."TenantId" IS NOT DISTINCT FROM m."TenantId";

UPDATE "AppFileCenterFileNodeTags" nt
SET "SpaceId" = coalesce(fn."SpaceId", tag."SpaceId")
FROM "AppFileCenterFileTags" tag
LEFT JOIN "AppFileCenterFileNodes" fn ON fn."Id" = nt."FileNodeId"
WHERE nt."SpaceId" IS NULL
  AND nt."TagId" = tag."Id"
  AND coalesce(fn."SpaceId", tag."SpaceId") IS NOT NULL;

UPDATE "AppFileCenterFileNodeTags" t
SET "SpaceId" = m."SpaceId"
FROM "_pcd_v2_space_owner_map" m
WHERE t."SpaceId" IS NULL
  AND t."OwnerId" = m."OwnerId"
  AND t."TenantId" IS NOT DISTINCT FROM m."TenantId";

UPDATE "AppFileCenterOperationLogs" log
SET "SpaceId" = fn."SpaceId"
FROM "AppFileCenterFileNodes" fn
WHERE log."SpaceId" IS NULL
  AND log."FileNodeId" = fn."Id"
  AND fn."SpaceId" IS NOT NULL;

UPDATE "AppFileCenterOperationLogs" log
SET "SpaceId" = ma."SpaceId"
FROM "AppFileCenterMediaAssets" ma
WHERE log."SpaceId" IS NULL
  AND log."MediaAssetId" = ma."Id"
  AND ma."SpaceId" IS NOT NULL;

-- -----------------------------------------------------------------------------
-- Step 6. 新索引。生产大表可改为 CREATE INDEX CONCURRENTLY 并拆到事务外执行。
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS "IX_AppSpaces_TenantId_OwnerUserId" ON "AppSpaces" ("TenantId", "OwnerUserId");
CREATE INDEX IF NOT EXISTS "IX_AppSpaces_TenantId_NormalizedName" ON "AppSpaces" ("TenantId", "NormalizedName");
CREATE UNIQUE INDEX IF NOT EXISTS "UX_AppSpaces_DefaultPersonal_Tenant" ON "AppSpaces" ("TenantId", "OwnerUserId") WHERE "IsDefaultPersonal" = true AND "IsDeleted" = false AND "TenantId" IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS "UX_AppSpaces_DefaultPersonal_Host" ON "AppSpaces" ("OwnerUserId") WHERE "IsDefaultPersonal" = true AND "IsDeleted" = false AND "TenantId" IS NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_AppSpaceMembers_Tenant_Space_User" ON "AppSpaceMembers" ("TenantId", "SpaceId", "UserId") WHERE "TenantId" IS NOT NULL AND "IsDeleted" = false;
CREATE UNIQUE INDEX IF NOT EXISTS "UX_AppSpaceMembers_Host_Space_User" ON "AppSpaceMembers" ("SpaceId", "UserId") WHERE "TenantId" IS NULL AND "IsDeleted" = false;
CREATE INDEX IF NOT EXISTS "IX_AppSpaceMembers_Tenant_User_Disabled" ON "AppSpaceMembers" ("TenantId", "UserId", "IsDisabled");
CREATE UNIQUE INDEX IF NOT EXISTS "UX_AppSpacePermissions_Tenant_Space_Role_Name" ON "AppSpacePermissions" ("TenantId", "SpaceId", "Role", "PermissionName") WHERE "TenantId" IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS "UX_AppSpacePermissions_Host_Space_Role_Name" ON "AppSpacePermissions" ("SpaceId", "Role", "PermissionName") WHERE "TenantId" IS NULL;

CREATE INDEX IF NOT EXISTS "IX_AppFileCenterFileNodes_TenantId_SpaceId_ParentId" ON "AppFileCenterFileNodes" ("TenantId", "SpaceId", "ParentId");
CREATE INDEX IF NOT EXISTS "IX_AppFileCenterFileNodes_TenantId_SpaceId_IsFavorite" ON "AppFileCenterFileNodes" ("TenantId", "SpaceId", "IsFavorite");
CREATE INDEX IF NOT EXISTS "IX_AppFileCenterBlobObjects_TenantId_SpaceId" ON "AppFileCenterBlobObjects" ("TenantId", "SpaceId");
CREATE INDEX IF NOT EXISTS "IX_AppFileCenterUploadSessions_TenantId_SpaceId_Status" ON "AppFileCenterUploadSessions" ("TenantId", "SpaceId", "Status");
CREATE INDEX IF NOT EXISTS "IX_AppFileCenterMediaAssets_TenantId_SpaceId_MediaType" ON "AppFileCenterMediaAssets" ("TenantId", "SpaceId", "MediaType");
CREATE INDEX IF NOT EXISTS "IX_AppFileCenterMediaAssets_TenantId_SpaceId_TakenAt" ON "AppFileCenterMediaAssets" ("TenantId", "SpaceId", "TakenAt");
CREATE INDEX IF NOT EXISTS "IX_AppFileCenterMediaAlbums_TenantId_SpaceId_NormalizedName" ON "AppFileCenterMediaAlbums" ("TenantId", "SpaceId", "NormalizedName");
CREATE INDEX IF NOT EXISTS "IX_AppFileCenterFileShares_TenantId_SpaceId_FileNodeId" ON "AppFileCenterFileShares" ("TenantId", "SpaceId", "FileNodeId");
CREATE INDEX IF NOT EXISTS "IX_AppFileCenterFileTags_TenantId_SpaceId_NormalizedName" ON "AppFileCenterFileTags" ("TenantId", "SpaceId", "NormalizedName");
CREATE INDEX IF NOT EXISTS "IX_AppFileCenterOperationLogs_TenantId_SpaceId_CreationTime" ON "AppFileCenterOperationLogs" ("TenantId", "SpaceId", "CreationTime");

-- 新唯一索引先以非唯一风险查询为准；实际替换旧 unique index 建议在 V2.0-App-B 稳定后单独迁移。
-- CREATE UNIQUE INDEX "UX_AppFileCenterFileNodes_Space_Parent_Name" ...

-- -----------------------------------------------------------------------------
-- Step 7. 添加 FK。若历史脏数据存在，本段会失败；dry-run 应先修复 FAIL 项。
-- -----------------------------------------------------------------------------
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_AppSpaceMembers_AppSpaces_SpaceId') THEN
        ALTER TABLE "AppSpaceMembers" ADD CONSTRAINT "FK_AppSpaceMembers_AppSpaces_SpaceId" FOREIGN KEY ("SpaceId") REFERENCES "AppSpaces"("Id") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_AppSpacePermissions_AppSpaces_SpaceId') THEN
        ALTER TABLE "AppSpacePermissions" ADD CONSTRAINT "FK_AppSpacePermissions_AppSpaces_SpaceId" FOREIGN KEY ("SpaceId") REFERENCES "AppSpaces"("Id") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_AppFileCenterFileNodes_AppSpaces_SpaceId') THEN
        ALTER TABLE "AppFileCenterFileNodes" ADD CONSTRAINT "FK_AppFileCenterFileNodes_AppSpaces_SpaceId" FOREIGN KEY ("SpaceId") REFERENCES "AppSpaces"("Id") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_AppFileCenterBlobObjects_AppSpaces_SpaceId') THEN
        ALTER TABLE "AppFileCenterBlobObjects" ADD CONSTRAINT "FK_AppFileCenterBlobObjects_AppSpaces_SpaceId" FOREIGN KEY ("SpaceId") REFERENCES "AppSpaces"("Id") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_AppFileCenterUploadSessions_AppSpaces_SpaceId') THEN
        ALTER TABLE "AppFileCenterUploadSessions" ADD CONSTRAINT "FK_AppFileCenterUploadSessions_AppSpaces_SpaceId" FOREIGN KEY ("SpaceId") REFERENCES "AppSpaces"("Id") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_AppFileCenterMediaAssets_AppSpaces_SpaceId') THEN
        ALTER TABLE "AppFileCenterMediaAssets" ADD CONSTRAINT "FK_AppFileCenterMediaAssets_AppSpaces_SpaceId" FOREIGN KEY ("SpaceId") REFERENCES "AppSpaces"("Id") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_AppFileCenterMediaAlbums_AppSpaces_SpaceId') THEN
        ALTER TABLE "AppFileCenterMediaAlbums" ADD CONSTRAINT "FK_AppFileCenterMediaAlbums_AppSpaces_SpaceId" FOREIGN KEY ("SpaceId") REFERENCES "AppSpaces"("Id") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_AppFileCenterFileShares_AppSpaces_SpaceId') THEN
        ALTER TABLE "AppFileCenterFileShares" ADD CONSTRAINT "FK_AppFileCenterFileShares_AppSpaces_SpaceId" FOREIGN KEY ("SpaceId") REFERENCES "AppSpaces"("Id") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_AppFileCenterFileTags_AppSpaces_SpaceId') THEN
        ALTER TABLE "AppFileCenterFileTags" ADD CONSTRAINT "FK_AppFileCenterFileTags_AppSpaces_SpaceId" FOREIGN KEY ("SpaceId") REFERENCES "AppSpaces"("Id") ON DELETE RESTRICT;
    END IF;
END $$;

-- -----------------------------------------------------------------------------
-- Step 8. 迁移后校验：执行方必须保存本段输出作为 after evidence。
-- -----------------------------------------------------------------------------
SELECT 'after.AppFileCenterFileNodes' AS metric, count(*) AS value FROM "AppFileCenterFileNodes"
UNION ALL SELECT 'after.AppFileCenterBlobObjects', count(*) FROM "AppFileCenterBlobObjects"
UNION ALL SELECT 'after.AppFileCenterUploadSessions', count(*) FROM "AppFileCenterUploadSessions"
UNION ALL SELECT 'after.AppFileCenterMediaAssets', count(*) FROM "AppFileCenterMediaAssets"
UNION ALL SELECT 'after.AppFileCenterMediaAlbums', count(*) FROM "AppFileCenterMediaAlbums"
UNION ALL SELECT 'after.AppFileCenterMediaAlbumItems', count(*) FROM "AppFileCenterMediaAlbumItems"
UNION ALL SELECT 'after.AppFileCenterFileShares', count(*) FROM "AppFileCenterFileShares"
UNION ALL SELECT 'after.AppFileCenterFileTags', count(*) FROM "AppFileCenterFileTags"
UNION ALL SELECT 'after.AppFileCenterFileNodeTags', count(*) FROM "AppFileCenterFileNodeTags"
UNION ALL SELECT 'after.AppFileCenterOperationLogs', count(*) FROM "AppFileCenterOperationLogs"
UNION ALL SELECT 'after.AppSpaces', count(*) FROM "AppSpaces"
UNION ALL SELECT 'after.AppSpaceMembers', count(*) FROM "AppSpaceMembers"
UNION ALL SELECT 'after.AppSpacePermissions', count(*) FROM "AppSpacePermissions";

SELECT 'null_space_id.AppFileCenterFileNodes' AS metric, count(*) AS value FROM "AppFileCenterFileNodes" WHERE "SpaceId" IS NULL
UNION ALL SELECT 'null_space_id.AppFileCenterBlobObjects', count(*) FROM "AppFileCenterBlobObjects" WHERE "SpaceId" IS NULL
UNION ALL SELECT 'null_space_id.AppFileCenterUploadSessions', count(*) FROM "AppFileCenterUploadSessions" WHERE "SpaceId" IS NULL
UNION ALL SELECT 'null_space_id.AppFileCenterMediaAssets', count(*) FROM "AppFileCenterMediaAssets" WHERE "SpaceId" IS NULL
UNION ALL SELECT 'null_space_id.AppFileCenterMediaAlbums', count(*) FROM "AppFileCenterMediaAlbums" WHERE "SpaceId" IS NULL
UNION ALL SELECT 'null_space_id.AppFileCenterMediaAlbumItems', count(*) FROM "AppFileCenterMediaAlbumItems" WHERE "SpaceId" IS NULL
UNION ALL SELECT 'null_space_id.AppFileCenterFileShares', count(*) FROM "AppFileCenterFileShares" WHERE "SpaceId" IS NULL
UNION ALL SELECT 'null_space_id.AppFileCenterFileTags', count(*) FROM "AppFileCenterFileTags" WHERE "SpaceId" IS NULL
UNION ALL SELECT 'null_space_id.AppFileCenterFileNodeTags', count(*) FROM "AppFileCenterFileNodeTags" WHERE "SpaceId" IS NULL
UNION ALL SELECT 'warn_null_space_id.AppFileCenterOperationLogs', count(*) FROM "AppFileCenterOperationLogs" WHERE "SpaceId" IS NULL;

SELECT 'duplicate_file_node_names_in_space' AS metric, count(*) AS value
FROM (
    SELECT "TenantId", "SpaceId", "ParentId", "NormalizedName", count(*)
    FROM "AppFileCenterFileNodes"
    WHERE "IsDeleted" = false
    GROUP BY "TenantId", "SpaceId", "ParentId", "NormalizedName"
    HAVING count(*) > 1
) d;

SELECT 'missing_default_personal_spaces' AS metric, count(*) AS value
FROM "_pcd_v2_space_owner_map" m
LEFT JOIN "AppSpaces" s ON s."Id" = m."SpaceId" AND s."IsDefaultPersonal" = true
WHERE s."Id" IS NULL;

\echo 'V2.0 Space migration: finished. Review all null/duplicate/missing metrics before production use.'
