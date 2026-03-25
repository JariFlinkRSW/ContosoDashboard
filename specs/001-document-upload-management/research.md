# Research: Document Upload and Management

## Source Alignment

- Decision: Treat the ASP.NET Core 10.0 Blazor file-upload guidance as the canonical framework reference for this feature.
- Rationale: The project targets .NET 10.0, and the ASP.NET Core 10.0 article is available. The core guidance used in this plan is unchanged in the areas we rely on: `InputFile`, `IBrowserFile`, `OpenReadStream(maxAllowedSize)`, non-cumulative file selection, direct streaming to disk, and avoiding whole-file buffering for non-small files.
- Alternatives considered: Continuing to rely on the ASP.NET Core 9.0 article was rejected because the repository target is .NET 10.0 and the current release documentation exists.

## Private File Serving Pattern

- Decision: Store files outside `wwwroot` and expose preview/download only through authorized application endpoints that load metadata first, enforce document access checks, and then return the file stream.
- Rationale: This preserves the current service-level authorization model, prevents public direct-object access, and keeps the same behavior when local file storage is later replaced by a cloud-backed implementation.
- Alternatives considered: Protected static-file middleware was rejected because per-document authorization is finer-grained than coarse authenticated access; storing files in `wwwroot` was rejected because obscured filenames are still effectively public; direct cloud URLs were deferred because the training baseline is offline-first.

## Local Storage Key Strategy

- Decision: Use a configured private root under the app content root and store a relative storage key such as `documents/{uploaderId}/{projectId-or-personal}/{yyyy}/{MM}/{guid}{ext}`. Persist the original filename separately for display only.
- Rationale: Relative storage keys are portable across machines, match the offline training constraint, work cleanly for later Azure blob names, and avoid collisions or path traversal by never trusting client filenames.
- Alternatives considered: Absolute filesystem paths were rejected because they bind records to one machine; original filenames in the saved path were rejected for collision and security reasons; a single flat directory was rejected because it scales poorly for cleanup and diagnostics.

## Upload and Replacement Workflow

- Decision: Validate metadata and authorization first, generate the storage key, save the file to storage, and only then create or update the metadata record. If the database operation fails after the file save, delete the saved file as compensating cleanup. For replacements, save the new file first, update the record second, and remove the old file only after the update succeeds.
- Rationale: SQL Server and the filesystem do not share a transaction boundary, so storage-first plus compensating cleanup is the simplest reliable way to avoid orphaned records and broken document references.
- Alternatives considered: Database-first insert with a pending row was rejected because it still needs reconciliation logic; full staged or quarantine workflows were rejected because dedicated malware scanning is out of scope for the first release; all-or-nothing multi-file batches were rejected because the spec expects per-file validation outcomes.

## Blazor Upload Component Behavior

- Decision: Treat each `IBrowserFile` as short-lived UI input. Capture metadata immediately, open a bounded read stream using the 25 MB limit, copy content to a service-facing stream or buffer, and then clear the file reference and reset the `InputFile` component after the upload batch completes. Track per-file UI states such as Selected, Uploading, Succeeded, and Failed.
- Rationale: The ASP.NET Core 10.0 guidance explicitly states that file selection is not cumulative, prior file references should not be treated as durable after reselection, and `OpenReadStream` defaults to a 500 KB limit unless an explicit `maxAllowedSize` is supplied. This design avoids disposed-stream and stale-reference issues common in Blazor Server, supports per-file progress and partial failures, and fits the training scale if uploads stay sequential or low-concurrency.
- Alternatives considered: Holding `IBrowserFile` references across rerenders was rejected as fragile; a custom JavaScript upload flow was rejected as a less natural fit for the current Blazor page architecture; high-concurrency parallel buffering was rejected because memory pressure scales badly on Blazor Server.

## Blazor Server Upload Limits and Concurrency

- Decision: Keep server-side upload processing sequential per user action, explicitly pass the 25 MB limit to `OpenReadStream`, and avoid changing SignalR `MaximumParallelInvocationsPerClient` from its default of `1`.
- Rationale: The ASP.NET Core 10.0 guidance warns that increasing `MaximumParallelInvocationsPerClient` can break Blazor Server file uploads and that many-file selections can hit the first-message SignalR size limit before streaming even begins. Sequential handling and bounded file counts are the simplest reliable baseline for this training app.
- Alternatives considered: Parallel file-stream reads per client were rejected because the framework guidance calls out reader-completion failures when concurrency is increased; relying on default file-size limits was rejected because the default 500 KB stream limit is far below the spec's 25 MB requirement.

## Validation Strategy Without an Existing Test Harness

- Decision: Make manual validation a first-class part of the plan using a repeatable checklist mapped to the spec's acceptance scenarios and edge cases, supplemented by temporary structured logging around upload, preview, download, authorization denial, replacement, delete, and share actions.
- Rationale: The repository currently has no automated test project, so the most credible immediate validation path is disciplined manual verification using the seeded roles and existing mock-login flow.
- Alternatives considered: Blocking implementation until a UI automation stack exists was rejected because it delays the feature without improving planning; unit tests only around storage were rejected because they would not validate the full auth and UI flow; unstructured exploratory testing was rejected because it misses partial-failure and authorization regressions.

## Storage Abstraction Contract

- Decision: Introduce a stream-oriented `IFileStorageService` whose operations are storage-key oriented, for example `SaveAsync`, `OpenReadAsync`, `DeleteAsync`, and a future optional read-link method for cloud storage.
- Rationale: This keeps authorization in the application layer, prevents local file paths from leaking into business logic, and allows the local and future cloud implementations to share the same document workflow and schema.
- Alternatives considered: A URL-centered abstraction was rejected because it pushes too much authorization behavior into infrastructure and complicates protected local preview/download flows.

## Authorization and Notification Reuse

- Decision: Reuse the current authorization pattern by enforcing access in a new `DocumentService` and extend the existing notification flow with document share and project-document events.
- Rationale: The codebase already centralizes authorization inside service methods and creates notifications directly from business services, so document features can integrate without changing the app's core architecture.
- Alternatives considered: UI-only authorization was rejected because the constitution requires defense in depth; a separate workflow engine was rejected because it adds complexity without supporting the training goal.
