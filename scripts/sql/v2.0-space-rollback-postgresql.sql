-- PrivateCloudDrive V2.0 Space 数据底座结构级回滚草案（PostgreSQL）
-- ADR: docs/adr/adr-04-v2-space-db-migration-rollback.md
-- 重要：仅在尚未产生 V2.0 非默认空间业务写入，或已归档/迁出新增空间数据后执行。
-- 推荐优先使用应用级回滚：旧代码忽略新增 Space 表/列，避免结构级丢数据。

\echo 'V2.0 Space rollback: start'

-- -----------------------------------------------------------------------------
-- Step 0. 回滚前安全检查。若存在非默认空间或非 Owner 成员，应先人工评估。
-- -----------------------------------------------------------------------------
SELECT 'non_default_spaces' AS metric, count(*) AS value
FROM "AppSpaces"
WHERE coalesce("IsDefaultPersonal", false) = false;

SELECT 'non_owner_members' AS metric, count(*) AS value
FROM "AppSpaceMembers"
WHERE "Role" <> 0 OR coalesce("IsDisabled", false) = true;

SELECT 'space_permissions' AS metric, count(*) AS value
FROM "AppSpacePermissions";

-- -----------------------------------------------------------------------------
-- Step 1. 删除 FK 约束。
-- -----------------------------------------------------------------------------
ALTER TABLE IF EXISTS "AppSpaceMembers" DROP CONSTRAINT IF EXISTS "FK_AppSpaceMembers_AppSpaces_SpaceId";
ALTER TABLE IF EXISTS "AppSpacePermissions" DROP CONSTRAINT IF EXISTS "FK_AppSpacePermissions_AppSpaces_SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterFileNodes" DROP CONSTRAINT IF EXISTS "FK_AppFileCenterFileNodes_AppSpaces_SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterBlobObjects" DROP CONSTRAINT IF EXISTS "FK_AppFileCenterBlobObjects_AppSpaces_SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterUploadSessions" DROP CONSTRAINT IF EXISTS "FK_AppFileCenterUploadSessions_AppSpaces_SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterMediaAssets" DROP CONSTRAINT IF EXISTS "FK_AppFileCenterMediaAssets_AppSpaces_SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterMediaAlbums" DROP CONSTRAINT IF EXISTS "FK_AppFileCenterMediaAlbums_AppSpaces_SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterFileShares" DROP CONSTRAINT IF EXISTS "FK_AppFileCenterFileShares_AppSpaces_SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterFileTags" DROP CONSTRAINT IF EXISTS "FK_AppFileCenterFileTags_AppSpaces_SpaceId";

-- -----------------------------------------------------------------------------
-- Step 2. 删除 V2.0 Space 相关索引。
-- -----------------------------------------------------------------------------
DROP INDEX IF EXISTS "IX_AppSpaces_TenantId_OwnerUserId";
DROP INDEX IF EXISTS "IX_AppSpaces_TenantId_NormalizedName";
DROP INDEX IF EXISTS "UX_AppSpaces_DefaultPersonal_Tenant";
DROP INDEX IF EXISTS "UX_AppSpaces_DefaultPersonal_Host";
DROP INDEX IF EXISTS "UX_AppSpaceMembers_Tenant_Space_User";
DROP INDEX IF EXISTS "UX_AppSpaceMembers_Host_Space_User";
DROP INDEX IF EXISTS "IX_AppSpaceMembers_Tenant_User_Disabled";
DROP INDEX IF EXISTS "UX_AppSpacePermissions_Tenant_Space_Role_Name";
DROP INDEX IF EXISTS "UX_AppSpacePermissions_Host_Space_Role_Name";
DROP INDEX IF EXISTS "IX_AppFileCenterFileNodes_TenantId_SpaceId_ParentId";
DROP INDEX IF EXISTS "IX_AppFileCenterFileNodes_TenantId_SpaceId_IsFavorite";
DROP INDEX IF EXISTS "IX_AppFileCenterBlobObjects_TenantId_SpaceId";
DROP INDEX IF EXISTS "IX_AppFileCenterUploadSessions_TenantId_SpaceId_Status";
DROP INDEX IF EXISTS "IX_AppFileCenterMediaAssets_TenantId_SpaceId_MediaType";
DROP INDEX IF EXISTS "IX_AppFileCenterMediaAssets_TenantId_SpaceId_TakenAt";
DROP INDEX IF EXISTS "IX_AppFileCenterMediaAlbums_TenantId_SpaceId_NormalizedName";
DROP INDEX IF EXISTS "IX_AppFileCenterFileShares_TenantId_SpaceId_FileNodeId";
DROP INDEX IF EXISTS "IX_AppFileCenterFileTags_TenantId_SpaceId_NormalizedName";
DROP INDEX IF EXISTS "IX_AppFileCenterOperationLogs_TenantId_SpaceId_CreationTime";

-- 若后续迁移已创建 Space 唯一索引，也在这里删除。
DROP INDEX IF EXISTS "UX_AppFileCenterFileNodes_Space_Parent_Name";
DROP INDEX IF EXISTS "UX_AppFileCenterFileNodes_Space_Root_Name";
DROP INDEX IF EXISTS "UX_AppFileCenterFileTags_Space_NormalizedName";

-- -----------------------------------------------------------------------------
-- Step 3. 删除现有表新增列。旧 OwnerId 数据保留，因此 V1.x 应用可继续工作。
-- -----------------------------------------------------------------------------
ALTER TABLE IF EXISTS "AppFileCenterOperationLogs" DROP COLUMN IF EXISTS "OperatorSpaceRole";
ALTER TABLE IF EXISTS "AppFileCenterOperationLogs" DROP COLUMN IF EXISTS "SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterFileNodeTags" DROP COLUMN IF EXISTS "SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterFileTags" DROP COLUMN IF EXISTS "SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterFileShares" DROP COLUMN IF EXISTS "SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterMediaAlbumItems" DROP COLUMN IF EXISTS "SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterMediaAlbums" DROP COLUMN IF EXISTS "SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterMediaAssets" DROP COLUMN IF EXISTS "SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterUploadSessions" DROP COLUMN IF EXISTS "SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterBlobObjects" DROP COLUMN IF EXISTS "SpaceId";
ALTER TABLE IF EXISTS "AppFileCenterFileNodes" DROP COLUMN IF EXISTS "SpaceId";

-- -----------------------------------------------------------------------------
-- Step 4. 删除 Space 表。
-- 注意：这会删除所有空间、成员、权限配置数据。
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS "AppSpacePermissions";
DROP TABLE IF EXISTS "AppSpaceMembers";
DROP TABLE IF EXISTS "AppSpaces";

-- -----------------------------------------------------------------------------
-- Step 5. 回滚后计数。执行方应与迁移前 baseline 比对核心表记录数。
-- -----------------------------------------------------------------------------
SELECT 'rollback.AppFileCenterFileNodes' AS metric, count(*) AS value FROM "AppFileCenterFileNodes"
UNION ALL SELECT 'rollback.AppFileCenterBlobObjects', count(*) FROM "AppFileCenterBlobObjects"
UNION ALL SELECT 'rollback.AppFileCenterUploadSessions', count(*) FROM "AppFileCenterUploadSessions"
UNION ALL SELECT 'rollback.AppFileCenterMediaAssets', count(*) FROM "AppFileCenterMediaAssets"
UNION ALL SELECT 'rollback.AppFileCenterMediaAlbums', count(*) FROM "AppFileCenterMediaAlbums"
UNION ALL SELECT 'rollback.AppFileCenterMediaAlbumItems', count(*) FROM "AppFileCenterMediaAlbumItems"
UNION ALL SELECT 'rollback.AppFileCenterFileShares', count(*) FROM "AppFileCenterFileShares"
UNION ALL SELECT 'rollback.AppFileCenterFileTags', count(*) FROM "AppFileCenterFileTags"
UNION ALL SELECT 'rollback.AppFileCenterFileNodeTags', count(*) FROM "AppFileCenterFileNodeTags"
UNION ALL SELECT 'rollback.AppFileCenterOperationLogs', count(*) FROM "AppFileCenterOperationLogs";

\echo 'V2.0 Space rollback: finished. If any non-default space existed, verify archived data before closing incident.'
