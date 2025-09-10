# Agent Workflow Orchestrator – Coding Guidelines

This document defines the **vision**, **project scope**, **styling rules**, **coding standards**, and **constraints** for building the Agent Workflow Orchestrator using **ASP.NET Core MVC / Razor Pages**, **Bootstrap 5**, and **SQLite**.

---

## Vision

The Agent Workflow Orchestrator helps teams **design, run, and monitor multi-agent workflows**.  
It provides a **visual drag-and-drop builder**, **execution monitoring**, and **human-in-the-loop collaboration**, focusing on clarity, extensibility, and developer adoption.

---

## Project Scope (MVP)

- User authentication & profiles.
- Visual workflow builder (drag-and-drop).
- Basic agent library (2–3 demo agents).
- Workflow execution with logging.
- Real-time execution monitoring (via SignalR).
- Basic workflow sharing/collaboration.

---

## Target Group

- AI developers, automation engineers, research teams.
- Enterprises exploring orchestration of multiple agents.
- Teams needing transparency in automated workflows.

---

## Styling Guidelines

### Framework
- Use **Bootstrap 5** for frontend styling.
- Prefer Bootstrap utility classes and components over custom CSS.
- Extend Bootstrap via custom CSS only if necessary (e.g., custom theme colors).

### Color Palette
- **Primary:** `#0d6efd` → actions, highlights
- **Secondary:** `#198754` → success states
- **Danger:** `#dc3545` → errors
- **Neutral:**
    - Background: `#f8f9fa`
    - Text dark: `#212529`
    - Text muted: `#6c757d`

### Components
- Buttons: `btn btn-primary`, `btn btn-secondary`, `btn btn-danger`
- Cards: `card shadow-sm rounded`
- Forms: `form-control`, `form-label`, `form-select`, `form-check`
- Modals: use Bootstrap modal components for agent configuration or collaborator sharing
- Workflow canvas: full-width scrollable div with Bootstrap grid/flex layout

---

## Clean Architecture

### Layers
1. **Domain Layer (Core)**
    - Entities: `Workflow`, `Agent`, `Execution`, `User`
    - Business logic only, interfaces for repositories/services

2. **Application Layer (Use Cases)**
    - Implements core workflows and orchestration logic (`ExecuteWorkflow`, `CreateWorkflow`)
    - Depends only on domain interfaces

3. **Infrastructure Layer**
    - EF Core repositories for SQLite
    - Logging, authentication, SignalR hubs

4. **Presentation Layer (ASP.NET Core MVC / Razor Pages)**
    - Controllers, Razor Pages, ViewModels
    - Uses Bootstrap 5 for UI

**Dependency Rule:** inner layers never depend on outer layers.

---

## Backend (ASP.NET Core + EF Core + SQLite)

### Structure
- `Domain/` → entities, interfaces
- `Application/` → use case services
- `Infrastructure/` → EF Core context, repositories, auth, SignalR
- `Presentation/` → Controllers, Razor Pages, ViewModels
- `Tests/` → unit and integration tests

### Conventions
- IDs: `Guid`
- Entities: include `CreatedAt`, `UpdatedAt`
- EF Core migrations for SQLite
- Authentication: ASP.NET Identity with cookies
- SignalR for live workflow execution updates

### Testing
- xUnit + Moq for unit tests
- Integration tests with `WebApplicationFactory`
- Target ~70% coverage for MVP

---

## Frontend (Razor Pages + Bootstrap)

### Structure
- `Pages/` → page-level views (Dashboard, Workflow Builder, Execution Monitor)
- `Shared/` → reusable components (Button, Card, Modal)

### Conventions
- Use Bootstrap utility classes and components in Razor Pages
- Components should be reusable and modular
- State management via Dependency Injection services (e.g., `WorkflowService`)

### Testing
- Unit tests with **bUnit**
- Optional E2E tests with Playwright

---

## Git & Collaboration

- Branch naming: `feature/`, `fix/`, `chore/`
- Conventional commit messages
- PRs must pass lint + tests

---

## Tooling

- .NET 8 SDK, EF Core, ASP.NET Core MVC / Razor Pages
- SQLite database
- Bootstrap 5
- xUnit, bUnit, Moq for testing
- GitHub Actions for CI/CD

---

## Constraints

- SQLite only for MVP
- Single-node deployment
- Maximum ~20 concurrent workflow executions
- Security: HTTPS, Identity cookie-based auth

---

## Non-Goals (MVP)

- Multi-tenancy
- AI-driven workflow suggestions
- Third-party agent marketplace
- Advanced analytics dashboards
- Mobile-first optimization
