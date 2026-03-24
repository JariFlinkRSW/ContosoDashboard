<!--
Sync Impact Report
- Version change: template -> 1.0.0
- Modified principles:
	- Template Principle 1 -> I. Training-First Boundaries
	- Template Principle 2 -> II. Security by Default in Training Scope
	- Template Principle 3 -> III. Offline-First Infrastructure Abstractions
	- Template Principle 4 -> IV. Spec-Driven Incremental Delivery
	- Template Principle 5 -> V. Layered Blazor Architecture
- Added sections:
	- Technical Guardrails
	- Delivery Workflow & Quality Gates
- Removed sections:
	- None
- Templates requiring updates:
	- ✅ .specify/templates/plan-template.md
	- ✅ .specify/templates/spec-template.md
	- ✅ .specify/templates/tasks-template.md
	- ✅ README.md
	- ✅ Review completed for .specify/templates/commands/*.md (no files present)
- Follow-up TODOs:
	- None
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-First Boundaries
The default implementation MUST remain suitable for offline training: local
dependencies only, explicit labeling of mock or non-production components, and no
required cloud services to run the core learning experience. Changes MUST NOT
represent the sample as production-ready or remove documented training
limitations without updating the repository guidance. Rationale: this repository
exists to teach spec-driven development in a portable environment, so training
reliability takes precedence over production realism.

### II. Security by Default in Training Scope
Protected features MUST enforce authorization in both the UI entry point and the
service or data-access path that performs the action. Features that handle
user-owned data, project-scoped data, or uploaded files MUST prevent insecure
direct object references, validate inputs with allowlists where practical, and
keep non-public assets outside direct web access. Security-relevant actions MUST
produce auditable logs or equivalent trace records. Rationale: training code is
allowed to be simplified, but it must still model correct security boundaries.

### III. Offline-First Infrastructure Abstractions
New infrastructure dependencies such as file storage, identity, messaging, or
external APIs MUST be introduced behind interfaces and dependency injection.
Every new infrastructure capability MUST have a local or offline implementation
as the default training path, and planning artifacts MUST document how a cloud
implementation could replace it without changing business logic. Rationale: the
project deliberately teaches separation of concerns and cloud migration paths
without making cloud access a prerequisite.

### IV. Spec-Driven Incremental Delivery
Feature work MUST start from a specification, implementation plan, and task list
that map back to independently valuable user stories. Specifications MUST define
acceptance scenarios, measurable outcomes, affected roles, and relevant failure
modes. Tasks MUST include validation for changed behavior, using automated tests
when a practical harness exists and explicit manual verification steps when it
does not. Rationale: the repository is a Spec Kit training project, so delivery
must remain traceable from stakeholder need to verified increment.

### V. Layered Blazor Architecture
Blazor pages and components MUST delegate business rules to services; services
MUST own orchestration, authorization enforcement, and persistence access; and
the Entity Framework Core data model MUST remain the source of truth for stored
state. Changes SHOULD extend existing layers before introducing new frameworks,
cross-cutting abstractions, or architectural seams, and any exception MUST be
justified in the implementation plan's complexity tracking. Rationale: the
training value of this codebase depends on keeping a clear, teachable structure.

## Technical Guardrails

- The application runtime MUST stay aligned with the target framework and ASP.NET
	Core Blazor Server stack declared in the repository.
- Default persistence MUST remain local and offline-capable for training,
	currently SQL Server LocalDB via Entity Framework Core unless a spec-approved
	change explicitly revises that baseline.
- Authentication and authorization changes MUST preserve a runnable local
	training mode, even when a migration path to Microsoft Entra ID or another
	production identity provider is documented.
- File-based capabilities MUST store private content outside `wwwroot`, use
	portable relative paths or equivalent opaque identifiers, and avoid direct use
	of user-supplied file names as storage keys.
- User-facing behavior, setup steps, and notable training limitations MUST be
	reflected in repository guidance when changed.

## Delivery Workflow & Quality Gates

- Every `/speckit.plan` output MUST pass a constitution check that confirms
	training scope, security boundaries, infrastructure abstraction, independent
	story slicing, and architectural fit before implementation begins.
- Every `/speckit.specify` output MUST document role impacts, authorization
	expectations, offline behavior, validation constraints, and observable edge
	cases for the feature.
- Every `/speckit.tasks` output MUST include work for security enforcement,
	infrastructure abstraction, documentation updates, and verification whenever
	the feature affects those areas.
- Pull requests or equivalent review packages MUST cite the governing spec, plan,
	and tasks artifacts and MUST explain any approved constitution exceptions.
- Documentation changes are REQUIRED when a feature changes setup, permissions,
	user workflow, or migration guidance.

## Governance

- This constitution supersedes conflicting workflow guidance elsewhere in the
	repository.
- Amendments MUST update this file and every affected template or guidance
	document in the same change.
- Compliance review is REQUIRED during planning and again during implementation
	review before work is considered complete.
- Versioning policy for this constitution follows semantic versioning:
	- MAJOR for removing or materially redefining a principle or governance rule.
	- MINOR for adding a new principle, section, or materially expanded mandate.
	- PATCH for clarifications, wording improvements, or non-semantic edits.
- Ratification records the first date this project-specific constitution was
	adopted. Last amended records the most recent approved content change.

**Version**: 1.0.0 | **Ratified**: 2026-03-24 | **Last Amended**: 2026-03-24
