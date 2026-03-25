# Quickstart: Document Upload and Management

## Purpose

Provide a repeatable implementation-validation flow for the planned document feature in the repository's local training environment.

## Prerequisites

- .NET 10.0 SDK installed
- SQL Server LocalDB available
- Repository on branch `001-document-upload-management`
- Local private storage root configured for document files under the app content root (for example `AppData/Documents`)

## Launch the App

1. From the repository root, run `dotnet run --project ContosoDashboard/ContosoDashboard.csproj`.
2. Open the local application URL shown in the console.
3. Sign in with one of the seeded training users.

## Story 1 Validation: Personal Documents MVP

1. Sign in as an Employee user.
2. Open the document management page.
3. Upload a supported PDF under 25 MB with required title and category.
4. Confirm the upload reports success.
5. Confirm the document appears in My Documents with title, category, upload date, file size, and project metadata.
6. Sort and filter the document list.
7. Search by title and tag.
8. Preview the uploaded PDF inline.
9. Download the document.
10. Update metadata and confirm the changes appear in the list.
11. Replace the document with another supported file and confirm the document remains available.
12. Delete the document after confirmation and confirm it no longer appears.

## Story 1 Negative Cases

1. Upload an unsupported file type and confirm rejection with a clear reason.
2. Upload a file over 25 MB and confirm rejection with a clear reason.
3. Upload a mixed-validity multi-file batch and confirm per-file outcomes rather than a misleading global success.
4. Simulate a storage failure and confirm no accessible document is left behind.

## Story 2 Validation: Project and Shared Documents

1. Sign in as a current project member and upload a document to the project.
2. Sign in as another authorized project participant and confirm the document is visible and downloadable.
3. Sign in as a non-member and confirm the project document is not visible in search or direct access attempts.
4. Share a document with a specific user and confirm the recipient receives a notification and sees the document in Shared with Me.
5. Share a document with the Engineering department/team scope and confirm users in that scope can see it.
6. Sign in as a Team Lead and confirm team-scoped management applies only to documents uploaded by users in the same department/team scope.
7. Sign in as a Project Manager and confirm project-scoped management actions work only within projects they manage.

## Story 3 Validation: Task and Dashboard Integration

1. Open a task with a project association.
2. Upload or attach a document from the task context.
3. Confirm the document is associated with the same project.
4. Open the dashboard and confirm recent documents and document counts reflect accessible documents.
5. Confirm project-document notifications appear for affected users.

## Story 4 Validation: Audit and Reporting

1. Perform upload, preview, download, share, replace, and delete actions.
2. Confirm audit records are written for the required actions.
3. Sign in as an Administrator and confirm reporting surfaces document type, uploader, and access-pattern summaries.

## Logging Expectations

- Log successful and denied access to preview and download flows.
- Log upload validation failures separately from storage failures.
- Log compensating cleanup when file save or metadata persistence partially fails.

## Exit Criteria

- All acceptance scenarios in [spec.md](./spec.md) have been manually validated.
- All critical edge cases in the specification have been exercised.
- No unauthorized document content is exposed through list, search, preview, download, or direct URL access.
