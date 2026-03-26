# CodeFabric — Copilot Instructions

You are working on **CodeFabric**, a .NET-based workflow orchestrator that drives agentic AI
execution pipelines. The system coordinates specialized VS Code agent personas through a
deterministic phase-gated workflow, with human approval checkpoints between phases.

---

## Project overview

CodeFabric automates the execution of a structured feature-delivery workflow composed of three
sequential phases: **Design**, **Implementation**, and **Test**. Each phase contains ordered steps
executed by named VS Code agents. 

Phase transitions require explicit approval.

The architecture is designed to be extended with additional workflow types
without modifying the engine.

---

## Solution structure

```
CodeFabric.slnx
├── CodeFabric.Core/          # Core logic & interfaces — no external dependencies
│   ├── Contracts/
│   ├── Definitions/
│   │   └── FeatureWorkflow.cs
│   └── Engine/
│       ├── IWorkflowDefinition.cs
│       ├── StepDefinition.cs
│       └── WorkflowStep.cs
│
├── CodeFabric.Storage/        # JSON persistence — implements IWorkflowStorage
│   └── JsonWorkflowStorage.cs
│
└── CodeFabric/                # CLI entry point 
    └── Program.cs
```

**Dependency rule (strictly enforced):**
- `CodeFabric.Core` references nothing outside the BCL.
- All other projects (`CodeFabric.Storage`, `CodeFabric`) reference `CodeFabric.Core`.
- No project may introduce circular references.

---

## Core abstractions

### `IWorkflowDefinition`

The single contract that encapsulates all workflow-specific knowledge. The engine knows nothing about
phases, step names, agents, or ordering — that all lives here.


### `WorkflowEngine`

The orchestrator that drives workflow execution. It is completely generic and reusable across any workflow definition.

### `IWorkflowStorage`

Abstraction for persisting workflow state. The engine is agnostic to the storage format or location.
---

## Agent invocation

Steps are executed by spawning VS Code CLI as a child process:

```
code --agent=<agentName>
     --model=claude-sonnet-4.6
     --allow-all-tools
     --autopilot
     -p "<rendered prompt>"
```

---


## Workflow state — `.workflow/context.json`

The context file is the single source of truth for resumability. It is written after every step
transition. On startup, the engine checks for an existing context file and resumes from the last
known state rather than restarting.

```json
{
  "workflowId": "feature-42-20250324-1430",
  "workflowType": "FeatureWorkflow",
  "input": { "issueNumber": "42", "repositoryPath": "C:/repos/myapp", "branch": "feature/42" },
  "currentPhase": "Design",
  "currentStep": "ReviewHighLevelDesign",
  "status": "Running",
  "startedAt": "2025-03-24T14:30:00Z",
  "completedAt": null,
  "history": [
    {
      "phase": "Design",
      "stepName": "HighLevelDesign",
      "status": "Completed",
      "artifactPath": "outputs/design/hld.md",
      "startedAt": "2025-03-24T14:30:05Z",
      "completedAt": "2025-03-24T15:02:10Z",
      "failureReason": null
    }
  ]
}
```

---

## Coding standards

### General
- Target **.NET 10**. Use C# 14 features where they improve clarity: primary constructors,
  collection expressions, `required` members, pattern matching, `field` keyword, `extension` blocks.
- Prefer `sealed` classes unless inheritance is explicitly designed for.
- Use `record` or `readonly record struct` for value objects and DTOs.
- All public APIs must have XML doc comments.
- No `static` state except for compile-time constants and read-only collections.

### Async
- All I/O operations are `async`/`await` end-to-end.
- Method signatures that perform I/O must accept and forward `CancellationToken ct`.
- Never use `.Result` or `.Wait()` — no sync-over-async.
- Use `ConfigureAwait(false)` in library code (`Core`, `Storage`). Not required in the CLI.

### Service registration and Dependency injection

Uses AppBoot for service registration. No static service locators or singletons. Constructor injection only.

Use `[Service(typeof(IInterface), ServiceLifetime)]` from `AppBoot/DependencyInjection`:
```csharp
[Service(typeof(IOrderingService), ServiceLifetime.Transient)]
class OrderingService(IRepository repository) : IOrderingService
```
- **Always** register by interface, not concrete type
- Attribute discovered automatically by `ServiceRegistrationBehavior`
- Default lifetime is `Transient` if omitted
---
