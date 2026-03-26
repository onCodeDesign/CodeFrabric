# High-Level Design

## Vision statement

I want to implement an orchestrator for my process.
The goal is to have a semi-automated process that implements a feature following below description.
The process is a combination of iterative steps grouped into incremental phases.
The incremental phases are:
-  Design
-Implementation
- Test
Each phase builds on previous, and it does not change it to account for progress. Each phase completion is approved by a human, and should have a clear commit marked with HUMAN:Approve
As a future version, event the approval may be done by an agent, so the process becomes fully autonomous.
Each phase consists of iterative steps, which are autonomously executed by specialized agents. The steps are:
* Design
   * High Level Design
   * Review High Level Design
   * Detailed Design
   * Review Detailed Design
* Implementation
   * Code Implementation
   * Unit Testing
   * Code Review
* Test
   * Integration Testing
   * Review Integration Testing   
   * User Acceptance Testing
   * Review User Acceptance Testing

The orchestrator should manage the execution of these steps, by executing Github Copilot CLI instances that perform the tasks, and waiting for their completion before moving to the next step. The orchestrator should also handle the approval process, by waiting for a commit with the message HUMAN:Approve before moving to the next phase.

The orchestrator is a hybrid, with a deterministic spine
The reasoning is grounded in your own workflow description from the document — you referenced "human-in-the-loop decisions," "git flow for logical units of work," and "close supervision still required." All of that maps cleanly to a deterministic orchestrator as the outer shell, with AI handling the inner execution of each step.

## Components break down

**Deterministic spine (.NET Console app):**
- A `PhaseStateMachine` class managing `Design → Implementation → Test` transitions
- A `StepRunner` that launches `gh copilot` CLI processes per step and captures output
- A `GitCommitWatcher` polling or hooking the repo for `HUMAN:Approve` commits
- A `WorkflowContext` object that persists phase/step state to disk (JSON file or SQLite), so the orchestrator survives restarts

**Agent execution layer:**
- Each step maps to a named agent invocation with a specific system prompt and instructions file
- The step worker writes outputs to a well-known folder (e.g. `.workflow/outputs/design/hld.md`), which becomes context for the next step
- Step completion is detected by exit code + output file presence, not by AI reasoning

**Observation layer:**
- A file watcher or Git polling loop detects the approval commit
- A result store accumulates step outputs so later agents get prior context
- Logs are structured (JSON to stdout or a file) so you can replay or audit any run

## A question before we go furtherYour answers will shape the detailed design significantly — particularly the step runner strategy, since `gh copilot` CLI has specific constraints around interactivity and process spawning that we'll need to work around cleanly. Once you answer, we can move straight into the interface design and project structure.