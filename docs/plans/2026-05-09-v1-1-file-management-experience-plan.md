# PrivateCloudDrive V1.1 File Management Experience Implementation Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Deliver V1.1 file management experience improvements: search, sorting/filtering, batch operations, storage usage display, and share management UX.

**Architecture:** Implement backend query/command capabilities first with TDD, then wire MAUI screens to the expanded contracts. Keep changes inside FileCenter application contracts/application/domain repository/EF repository/HTTP controllers and MAUI client/pages. Preserve current behavior when new query parameters are omitted.

**Tech Stack:** ABP Framework, .NET 10, EF Core, PostgreSQL, .NET MAUI, Shouldly/xUnit tests.

---

## Phase 1: Backend search, sorting, filtering, and storage usage

### Task 1: Extend folder list input contract

**Objective:** Add query fields for keyword search, search scope, node type, media type, and sorting.

**Files:**
- Modify: `aspnet-core/src/PrivateCloudDrive.Application.Contracts/FileCenter/GetFolderChildrenInput.cs`
- Create: `aspnet-core/src/PrivateCloudDrive.Application.Contracts/FileCenter/FileCenterSearchScope.cs`
- Create: `aspnet-core/src/PrivateCloudDrive.Application.Contracts/FileCenter/FileCenterMediaTypeFilter.cs`
- Test: `aspnet-core/test/PrivateCloudDrive.EntityFrameworkCore.Tests/EntityFrameworkCore/FileCenter/EfCoreFileCenterFoldersAppServiceTests.cs`

**Fields:**
- `string? SearchKeyword`
- `FileCenterSearchScope SearchScope = FileCenterSearchScope.CurrentFolder`
- `FileNodeType? NodeType`
- `FileCenterMediaTypeFilter? MediaType`
- `string? Sorting`

**TDD:** Add tests that prove old list behavior still works when new fields are omitted.

### Task 2: Extend repository query contract

**Objective:** Pass the new query options through `IFileNodeRepository`.

**Files:**
- Modify: `aspnet-core/src/PrivateCloudDrive.Domain/FileCenter/IFileNodeRepository.cs`
- Modify: `aspnet-core/src/PrivateCloudDrive.EntityFrameworkCore/FileCenter/EfCoreFileNodeRepository.cs`

**Requirements:**
- Search keyword filters by `Name` or `NormalizedName` using case-insensitive contains where provider permits.
- Current-folder search keeps `ParentId == input.ParentId`.
- All-files search ignores ParentId but stays within owner + tenant + not deleted.
- NodeType filter supports folder/file.
- MediaType filter uses content type / file node type for Image, Video, Other.
- Sorting allowlist only; invalid sorting falls back to default.
- Default sorting remains folder first + normalized name ascending.

### Task 3: Add backend tests for search and sorting

**Objective:** Validate query behavior and prevent cross-user data leakage.

**Tests:**
- `Should_Search_Folders_By_Keyword_In_Current_Folder`
- `Should_Search_All_User_Nodes_When_SearchScope_Is_All`
- `Should_Not_Return_Other_User_Nodes_When_Searching_All`
- `Should_Filter_By_NodeType`
- `Should_Sort_By_Name_Size_CreationTime`
- `Should_Fallback_To_Default_Sorting_When_Invalid`

**Command:**

```bash
dotnet test aspnet-core/test/PrivateCloudDrive.EntityFrameworkCore.Tests/PrivateCloudDrive.EntityFrameworkCore.Tests.csproj --no-restore --filter "FullyQualifiedName~EfCoreFileCenterFoldersAppServiceTests"
```

### Task 4: Add storage usage contract and application service

**Objective:** Return current user's storage usage, quota, remaining bytes, and usage percent.

**Files:**
- Create: `aspnet-core/src/PrivateCloudDrive.Application.Contracts/FileCenter/StorageUsageDto.cs`
- Create/Modify: `aspnet-core/src/PrivateCloudDrive.Application.Contracts/FileCenter/IFileCenterStorageAppService.cs`
- Create: `aspnet-core/src/PrivateCloudDrive.Application/FileCenter/FileCenterStorageAppService.cs`
- Create/Modify controller if needed under `aspnet-core/src/PrivateCloudDrive.HttpApi/Controllers/FileCenter/`

**Rules:**
- Used bytes should count active file nodes for current user/tenant.
- Quota comes from `PrivateCloudDriveSettings.FileCenter.UserStorageQuotaInBytes`.
- Remaining = max(quota - used, 0).
- Percent rounded to 2 decimal places, avoid division by zero.
- Do not expose storage paths.

---

## Phase 2: Backend batch operations

### Task 5: Add batch input DTOs

**Files:**
- Create: `BatchFileNodeInput.cs`
- Create: `BatchMoveFileNodesInput.cs`
- Create: `BatchSetFavoriteInput.cs`

**Validation:**
- IDs required.
- Limit batch size to 100.
- Reject empty ID list.
- Deduplicate IDs server-side.

### Task 6: Add batch operations to folder app service

**Methods:**
- `DeleteManyAsync(BatchFileNodeInput input)`
- `RestoreManyAsync(BatchFileNodeInput input)`
- `PermanentDeleteManyAsync(BatchFileNodeInput input)`
- `MoveManyAsync(BatchMoveFileNodesInput input)`
- `SetFavoriteManyAsync(BatchSetFavoriteInput input)`

**Rules:**
- Each item must pass current owner/current tenant validation.
- Batch restore should fail clearly if any item has a restore conflict.
- Permanent delete must reuse existing cleanup logic.
- Moving a folder into itself or descendants must still be blocked by existing manager rules.

---

## Phase 3: MAUI search, sort/filter, and batch UI

### Task 7: Extend MAUI API client models and methods

**Files:**
- Modify: `maui/PrivateCloudDrive.App/Services/ICloudDriveApiClient.cs`
- Modify: `maui/PrivateCloudDrive.App/Services/CloudDriveApiClient.cs`
- Modify/create model classes under `maui/PrivateCloudDrive.App/Models/`

**Requirements:**
- Add query options for `GetItemsAsync`.
- Add batch delete/restore/permanent delete/move/favorite APIs.
- Add storage usage API.
- Add share list/disable/copy helper data.

### Task 8: Update Files page UI

**Files:**
- Modify: `maui/PrivateCloudDrive.App/Views/FilesPage.xaml`
- Modify: `maui/PrivateCloudDrive.App/Views/FilesPage.xaml.cs`

**Requirements:**
- Search box.
- Sort/filter button.
- Multi-select mode.
- Batch toolbar.
- Empty search state.
- Clear filter action.

### Task 9: Update Trash page UI

**Files:**
- Modify: `maui/PrivateCloudDrive.App/Views/TrashPage.xaml`
- Modify: `maui/PrivateCloudDrive.App/Views/TrashPage.xaml.cs`

**Requirements:**
- Multi-select restore.
- Multi-select permanent delete.
- Strong confirmation for permanent deletion.

---

## Phase 4: Capacity and share experience

### Task 10: Add capacity card to Settings page

**Files:**
- Modify: `maui/PrivateCloudDrive.App/Views/SettingsPage.xaml`
- Modify: `maui/PrivateCloudDrive.App/Views/SettingsPage.xaml.cs`

**Requirements:**
- Show used / quota / remaining.
- Show progress bar.
- Show warning when usage >= 90%.
- Show graceful error if API unavailable.

### Task 11: Add My Shares page or Settings section

**Files:**
- Create/Modify MAUI view for shares.
- Modify CloudDriveApiClient share methods.

**Requirements:**
- List current user's shares.
- Show file name, expiration, password-protected, allow download, enabled/expired status.
- Copy link.
- Disable share.

---

## Phase 5: Verification and release handoff

### Task 12: Verification

**Commands:**

```bash
dotnet test aspnet-core/test/PrivateCloudDrive.EntityFrameworkCore.Tests/PrivateCloudDrive.EntityFrameworkCore.Tests.csproj --no-restore --filter "FullyQualifiedName~FileCenter"
dotnet build aspnet-core/PrivateCloudDrive.slnx --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/verify-maui-build.ps1 -SkipAndroid -NoRestore
```

**Docs:**
- Update `docs/testing.md` V1.1 acceptance checklist.
- Update `docs/progress.md` with V1.1 status.
- Create `docs/release-notes-v1.1.md` after implementation.

---

## Out of scope

- Full-text content search.
- AI/OCR search.
- Recursive folder size aggregation.
- Desktop sync.
- Enterprise team permissions.
