# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/document-management.md, quickstart.md

**Tests**: No automated test project exists in this repository. Validation tasks below use the manual verification flow in `specs/001-document-upload-management/quickstart.md` and require logging checks where the feature introduces security-sensitive behavior.

**Organization**: Tasks are grouped by user story so each story can be implemented and verified as an independent increment.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the configuration and infrastructure seams required for private document storage in the existing Blazor Server app.

- [ ] T001 Configure the private document storage root and upload limits in ContosoDashboard/appsettings.json and ContosoDashboard/appsettings.Development.json
- [ ] T002 Create the storage abstraction and local implementation in ContosoDashboard/Services/IFileStorageService.cs and ContosoDashboard/Services/LocalFileStorageService.cs
- [ ] T003 Register document storage services, controller support, and document feature configuration in ContosoDashboard/Program.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the shared document schema, relationships, and service contracts that all user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T004 [P] Create the core document entities in ContosoDashboard/Models/Document.cs, ContosoDashboard/Models/DocumentShare.cs, ContosoDashboard/Models/DocumentActivityRecord.cs, and ContosoDashboard/Models/DocumentTag.cs
- [ ] T005 [P] Extend shared domain models for document relationships and notification types in ContosoDashboard/Models/User.cs, ContosoDashboard/Models/Project.cs, ContosoDashboard/Models/TaskItem.cs, and ContosoDashboard/Models/Notification.cs
- [ ] T006 Update EF Core mappings, DbSets, indexes, and relationship rules for documents in ContosoDashboard/Data/ApplicationDbContext.cs
- [ ] T007 Create the document-management schema migration in ContosoDashboard/Migrations/
- [ ] T008 Define the document service contract, DTOs, and command models in ContosoDashboard/Services/IDocumentService.cs

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - Upload and find my documents (Priority: P1) 🎯 MVP

**Goal**: Deliver personal document upload, browsing, preview, download, metadata editing, replacement, and deletion for authenticated employees.

**Independent Test**: Complete the Story 1 validation and negative-case flows in `specs/001-document-upload-management/quickstart.md`, including preview/download authorization and storage-failure cleanup checks.

### Validation for User Story 1

- [ ] T009 [P] [US1] Verify the personal-document upload, browse, search, preview, replace, and delete flow in specs/001-document-upload-management/quickstart.md
- [ ] T010 [P] [US1] Verify unsupported file, oversize file, mixed-validity batch, and storage-failure handling in specs/001-document-upload-management/quickstart.md

### Implementation for User Story 1

- [ ] T011 [US1] Implement personal document upload, search, metadata update, replacement, deletion, authorization, and audit logic in ContosoDashboard/Services/DocumentService.cs
- [ ] T012 [P] [US1] Implement protected preview and download endpoints for authorized personal documents in ContosoDashboard/Controllers/DocumentFilesController.cs
- [ ] T013 [P] [US1] Create the personal document management UI with upload progress, filters, search, and edit actions in ContosoDashboard/Pages/Documents.razor
- [ ] T014 [P] [US1] Add the document management navigation entry in ContosoDashboard/Shared/NavMenu.razor
- [ ] T015 [US1] Add document feature logging for upload validation, storage failures, preview, download, and delete operations in ContosoDashboard/Services/DocumentService.cs and ContosoDashboard/Controllers/DocumentFilesController.cs

**Checkpoint**: User Story 1 is fully functional and can be demonstrated as the MVP.

---

## Phase 4: User Story 2 - Work with project and shared documents (Priority: P2)

**Goal**: Allow project members to work with project documents and let authorized users share documents with specific users or department/team scope.

**Independent Test**: Complete the Story 2 project-member, non-member, share-notification, team-lead, and project-manager checks in `specs/001-document-upload-management/quickstart.md`.

### Validation for User Story 2

- [ ] T016 [P] [US2] Verify project-document visibility and unauthorized direct-access denial in specs/001-document-upload-management/quickstart.md
- [ ] T017 [P] [US2] Verify direct-user sharing, department/team sharing, and recipient notifications in specs/001-document-upload-management/quickstart.md

### Implementation for User Story 2

- [ ] T018 [US2] Extend ContosoDashboard/Services/DocumentService.cs with project-member upload rules, share commands, team-lead management, project-manager management, and share-triggered notifications
- [ ] T019 [P] [US2] Extend the shared and project-document experience in ContosoDashboard/Pages/Documents.razor for Shared with Me filters and share actions
- [ ] T020 [P] [US2] Add the project document section with project-context upload and management actions in ContosoDashboard/Pages/ProjectDetails.razor
- [ ] T021 [US2] Harden authorized access handling for shared and project documents in ContosoDashboard/Controllers/DocumentFilesController.cs and ContosoDashboard/Services/DocumentService.cs

**Checkpoint**: User Stories 1 and 2 work independently, with collaboration and scoped management now available.

---

## Phase 5: User Story 3 - Use documents inside existing work flows (Priority: P3)

**Goal**: Surface documents in task and dashboard flows so users encounter relevant documents where they already work.

**Independent Test**: Complete the Story 3 task-attachment, task-project inheritance, dashboard recent-documents, counts, and project-notification checks in `specs/001-document-upload-management/quickstart.md`.

### Validation for User Story 3

- [ ] T022 [P] [US3] Verify task-context upload, task document visibility, and inherited project association in specs/001-document-upload-management/quickstart.md
- [ ] T023 [P] [US3] Verify dashboard recent-document summaries, counts, and project-document notifications in specs/001-document-upload-management/quickstart.md

### Implementation for User Story 3

- [ ] T024 [US3] Extend ContosoDashboard/Services/DocumentService.cs with task-context upload/query support and dashboard summary queries
- [ ] T025 [P] [US3] Add task document attachment and related-document UI in ContosoDashboard/Pages/Tasks.razor
- [ ] T026 [P] [US3] Add recent-document widgets and document counts to the dashboard in ContosoDashboard/Pages/Index.razor
- [ ] T027 [US3] Extend ContosoDashboard/Services/TaskService.cs and ContosoDashboard/Services/DashboardService.cs to surface document-aware task and dashboard data

**Checkpoint**: User Stories 1, 2, and 3 are independently usable inside the main work surfaces of the app.

---

## Phase 6: User Story 4 - Review document activity and usage (Priority: P4)

**Goal**: Give administrators auditable activity views and reporting summaries for document usage and governance.

**Independent Test**: Complete the Story 4 audit-log and reporting checks in `specs/001-document-upload-management/quickstart.md` after exercising uploads, downloads, previews, shares, replacements, and deletes.

### Validation for User Story 4

- [ ] T028 [P] [US4] Verify audit records are created for upload, preview, download, share, replace, and delete flows in specs/001-document-upload-management/quickstart.md
- [ ] T029 [P] [US4] Verify administrator reporting for document types, active uploaders, and access patterns in specs/001-document-upload-management/quickstart.md

### Implementation for User Story 4

- [ ] T030 [US4] Extend ContosoDashboard/Services/DocumentService.cs with administrator audit queries and reporting summaries
- [ ] T031 [P] [US4] Add administrator-only audit and reporting sections to ContosoDashboard/Pages/Documents.razor

**Checkpoint**: All four user stories are fully functional, including governance and reporting.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Finish documentation, migration guidance, and end-to-end validation across all stories.

- [ ] T032 [P] Update feature setup and usage documentation in README.md and specs/001-document-upload-management/quickstart.md
- [ ] T033 Review document storage abstraction, migration notes, and operational assumptions in specs/001-document-upload-management/plan.md and specs/001-document-upload-management/research.md
- [ ] T034 Run the complete manual validation pass for all stories and edge cases in specs/001-document-upload-management/quickstart.md
- [ ] T035 Perform final security and log-review hardening for document flows in ContosoDashboard/Services/DocumentService.cs, ContosoDashboard/Controllers/DocumentFilesController.cs, and ContosoDashboard/Pages/Documents.razor

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup**: No dependencies; start immediately.
- **Phase 2: Foundational**: Depends on Phase 1 and blocks all story implementation.
- **Phase 3: User Story 1**: Depends on Phase 2 and establishes the base document workflows used everywhere else.
- **Phase 4: User Story 2**: Depends on Phase 3 because project and sharing flows extend the same document service, page, and file endpoint surfaces.
- **Phase 5: User Story 3**: Depends on Phase 4 because task and dashboard integrations build on project-aware document queries and notifications.
- **Phase 6: User Story 4**: Depends on Phases 3 through 5 because reporting is only useful once document activity data exists.
- **Phase 7: Polish**: Depends on all implemented stories.

### User Story Dependency Graph

- **US1 (P1)** -> base personal document workflows
- **US2 (P2)** -> depends on US1
- **US3 (P3)** -> depends on US2
- **US4 (P4)** -> depends on US1, US2, and US3

### Within Each User Story

- Run the listed validation tasks before calling the story complete.
- Complete service-layer logic before relying on UI or endpoint integrations.
- Keep authorization and audit behavior in the same service/controller changes that introduce new access paths.

### Parallel Opportunities

- T004 and T005 can run in parallel once the setup phase finishes.
- T012, T013, and T014 can run in parallel after T011 defines the base personal-document workflows.
- T019 and T020 can run in parallel after T018 defines project/share behavior.
- T025 and T026 can run in parallel after T024 defines task/dashboard document queries.
- T028 and T029 can run in parallel while validating the audit/reporting story.

---

## Parallel Example: User Story 1

```text
Task: T012 Implement protected preview and download endpoints in ContosoDashboard/Controllers/DocumentFilesController.cs
Task: T013 Create the personal document management UI in ContosoDashboard/Pages/Documents.razor
Task: T014 Add the document navigation entry in ContosoDashboard/Shared/NavMenu.razor
```

## Parallel Example: User Story 2

```text
Task: T019 Extend the shared-document experience in ContosoDashboard/Pages/Documents.razor
Task: T020 Add the project document section in ContosoDashboard/Pages/ProjectDetails.razor
```

## Parallel Example: User Story 3

```text
Task: T025 Add task document attachment UI in ContosoDashboard/Pages/Tasks.razor
Task: T026 Add recent-document widgets and counts in ContosoDashboard/Pages/Index.razor
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Deliver Phase 3 as the personal document MVP.
3. Run the Story 1 manual validation flow before moving on.

### Incremental Delivery

1. Deliver US1 for personal document management.
2. Add US2 for project and shared document collaboration.
3. Add US3 for task and dashboard integration.
4. Add US4 for audit and reporting.
5. Finish with Phase 7 documentation, security review, and full manual validation.

### Suggested MVP Scope

- Phase 1: Setup
- Phase 2: Foundational
- Phase 3: User Story 1
