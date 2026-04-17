---
status: planning
priority: high
due: 2026-06-01
tags:
  - project
  - backend
  - api
assignee: Team
---

# API Migration

Migrate the monolithic REST API to a modular architecture with improved observability.

## Goals

- Decompose monolith into domain-specific services
- Add OpenTelemetry instrumentation
- Implement API versioning strategy
- Maintain backward compatibility during transition

## Architecture

The new architecture uses:
- **ASP.NET Core** minimal APIs per domain
- **MCP servers** for tool integration (see [[Research - Local AI Models]])
- **Docker Model Runner** for local AI inference

## Timeline

| Phase | Target | Status |
|-------|--------|--------|
| Planning | April 2026 | In Progress |
| Core services | May 2026 | Not Started |
| Migration | June 2026 | Not Started |

## Dependencies

- Requires [[Website Redesign]] frontend to support new API endpoints
- Need updated auth middleware before service decomposition

#project #backend #api #architecture
