# Contract: Document Management Interfaces

## Purpose

Define the user-facing routes, protected file endpoints, and command contracts needed to implement document management inside the existing Blazor Server app.

## Page and Route Contracts

### `GET /documents`

- Audience: Authenticated users in Employee, TeamLead, ProjectManager, or Administrator roles
- Purpose: Display the user's personal documents and the documents shared with them in one page with filters, search, and upload entry points
- Required Behaviors:
  - Show title, category, upload date, file size, and associated project
  - Support sort by title, upload date, category, and file size
  - Support filter by category, project, and date range
  - Support search by title, description, tags, uploader, and project name
  - Separate My Documents and Shared with Me views or tabs

### `GET /projects/{projectId}` document section

- Audience: Authorized project members, project managers, administrators
- Purpose: Show all accessible documents associated with the project and allow project-context upload according to role rules
- Required Behaviors:
  - Show project documents list with uploader and category metadata
  - Allow current project members to upload documents to the project
  - Restrict stronger management actions to owner, team lead in department/team scope, project manager of that project, or administrator

### Task document interaction contract

- Surface: Existing tasks page and/or task detail interaction
- Purpose: Attach or upload a document in task context so the document inherits the task's project when present
- Required Behaviors:
  - Show documents related to the task
  - Allow authorized upload from task context
  - Preserve authorization parity with the linked document and project

## Protected File Endpoint Contracts

### `GET /document-files/{documentId}/download`

- Purpose: Return a file download for an authorized document
- Authorization:
  - Owner, explicit share recipient, department/team share recipient, entitled project participant, authorized team lead, authorized project manager, or administrator
- Response:
  - `200 OK` with attachment disposition and original filename when authorized
  - `404 Not Found` when the document does not exist or is intentionally hidden from unauthorized users
- Audit:
  - Record a document activity entry for successful download

### `GET /document-files/{documentId}/preview`

- Purpose: Stream an authorized document inline for previewable types
- Authorization: Same as download
- Response:
  - `200 OK` with inline disposition for previewable types (PDF, JPEG, PNG)
  - `400 Bad Request` or equivalent user-facing denial for unsupported preview types
  - `404 Not Found` when the document does not exist or is intentionally hidden from unauthorized users
- Audit:
  - Record a document activity entry for successful preview access

## Upload Command Contract

### `DocumentUploadCommand`

- Fields:
  - `Title` (required)
  - `Description` (optional)
  - `Category` (required)
  - `TagValues[]` (optional)
  - `ProjectId` (optional)
  - `TaskId` (optional)
  - `Files[]` (one or more selected files)
- Rules:
  - Each file is validated independently for type and size
  - Each file produces its own success/failure result
  - Uploading in task context must align `ProjectId` to the task's project when present

## Metadata Update Contract

### `UpdateDocumentMetadataCommand`

- Fields:
  - `DocumentId`
  - `Title`
  - `Description`
  - `Category`
  - `TagValues[]`
- Authorization:
  - Owner, authorized team lead within department/team scope, project manager of the associated project, or administrator

## Replace Document Contract

### `ReplaceDocumentFileCommand`

- Fields:
  - `DocumentId`
  - `ReplacementFile`
- Rules:
  - Replacement file must pass the same validation rules as a new upload
  - Original document remains intact if replacement storage or persistence fails

## Share Document Contract

### `ShareDocumentCommand`

- Fields:
  - `DocumentId`
  - `SharedWithUserId` (optional)
  - `SharedWithDepartment` (optional)
- Rules:
  - Exactly one target type is allowed per share command
  - Department/team shares use the existing app department values
  - Successful shares create notifications for affected recipients

## Delete Document Contract

### `DeleteDocumentCommand`

- Fields:
  - `DocumentId`
  - `Confirmation` (required affirmative intent)
- Rules:
  - Delete is permanent in the first release
  - Binary content and metadata are both removed when delete succeeds
  - Successful deletes create an activity record
