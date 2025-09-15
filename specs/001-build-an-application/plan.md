# Implementation Plan: Windows 11 Taskbar Network Traffic Monitor

**Branch**: `001-build-an-application` | **Date**: 2025-09-15 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/001-build-an-application/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
4. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
5. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, or `GEMINI.md` for Gemini CLI).
6. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
7. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
8. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
Primary requirement: Real-time network traffic monitoring application that integrates seamlessly into Windows 11 taskbar with minimalistic display and modern GUI for detailed statistics. Technical approach: Windows desktop application using native Windows APIs for taskbar integration and network monitoring capabilities.

## Technical Context
**Language/Version**: C# .NET 8 (WPF/WinUI 3) - optimal for Windows 11 taskbar integration  
**Primary Dependencies**: System.Net.NetworkInformation, Windows.UI, Microsoft.Toolkit.Win32.UI.Controls  
**Storage**: N/A (real-time only, no historical data persistence)  
**Testing**: MSTest/NUnit for unit testing, manual testing for taskbar integration  
**Target Platform**: Windows 11 desktop (requires Windows 11 specific taskbar APIs)  
**Project Type**: single - standalone Windows desktop application  
**Performance Goals**: Sub-100ms UI response, <1% CPU usage during monitoring, <50MB RAM usage  
**Constraints**: <100ms response to user interactions, minimal system resource consumption, seamless taskbar integration  
**Scale/Scope**: Single-user desktop application, monitor primary network adapter, real-time display updates

**User Input Context**: No specific tech stack preferences, requirement for seamless Windows 11 taskbar integration

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Simplicity**:
- Projects: 1 (desktop app with integrated monitoring and UI)
- Using framework directly? (Yes - direct .NET/WPF APIs, no custom wrappers)
- Single data model? (Yes - NetworkTrafficData entity only)
- Avoiding patterns? (Yes - direct API usage, no unnecessary abstraction layers)

**Architecture**:
- EVERY feature as library? (Yes - NetworkMonitor library, TaskbarIntegration library, UIComponents library)
- Libraries listed: 
  - NetworkMonitor: Real-time network traffic collection and calculation
  - TaskbarIntegration: Windows 11 taskbar display and integration
  - UIComponents: Modern GUI for detailed statistics display
- CLI per library: 
  - NetworkMonitor: --monitor --adapter [name] --format [json|text]
  - TaskbarIntegration: --show --hide --position [coordinates]
  - UIComponents: --display-stats --theme [auto|light|dark]
- Library docs: llms.txt format planned for each library

**Testing (NON-NEGOTIABLE)**:
- RED-GREEN-Refactor cycle enforced? (Yes - tests written first, must fail before implementation)
- Git commits show tests before implementation? (Yes - strict TDD workflow)
- Order: Contract→Integration→E2E→Unit strictly followed? (Yes)
- Real dependencies used? (Yes - actual network adapters, real Windows taskbar APIs)
- Integration tests for: new libraries, taskbar API integration, network monitoring accuracy
- FORBIDDEN: Implementation before test, skipping RED phase

**Observability**:
- Structured logging included? (Yes - performance metrics, error tracking, user interactions)
- Frontend logs → backend? (N/A - single desktop application)
- Error context sufficient? (Yes - detailed error logging with context)

**Versioning**:
- Version number assigned? (1.0.0 - MAJOR.MINOR.BUILD format)
- BUILD increments on every change? (Yes - automated versioning)
- Breaking changes handled? (Yes - parallel tests for API changes)

## Project Structure

### Documentation (this feature)
```
specs/001-build-an-application/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
# Option 1: Single project (DEFAULT)
src/
├── models/              # NetworkTrafficData, Configuration models
├── services/            # NetworkMonitorService, TaskbarService
├── cli/                 # Command-line interfaces for each library
└── lib/                 # NetworkMonitor, TaskbarIntegration, UIComponents libraries

tests/
├── contract/            # API contract tests
├── integration/         # Taskbar integration, network monitoring tests
└── unit/               # Individual component unit tests
```

**Structure Decision**: Option 1 - Single project (Windows desktop application with multiple internal libraries)

## Phase 0: Outline & Research
1. **Extract unknowns from Technical Context** above:
   - Windows 11 taskbar integration APIs and best practices
   - Real-time network monitoring approaches in .NET
   - WinUI 3 vs WPF for Windows 11 modern UI design
   - Performance optimization techniques for system tray applications
   - Windows theme integration and automatic theme detection

2. **Generate and dispatch research agents**:
   ```
   Task: "Research Windows 11 taskbar integration APIs for custom applications"
   Task: "Find best practices for .NET real-time network monitoring with minimal CPU usage"
   Task: "Research WinUI 3 vs WPF for Windows 11 modern desktop applications"
   Task: "Find performance optimization patterns for Windows system tray monitoring apps"
   Task: "Research Windows 11 theme detection and automatic UI theme switching"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with all technical decisions and rationale documented

## Phase 1: Design & Contracts
*Prerequisites: research.md complete*

1. **Extract entities from feature spec** → `data-model.md`:
   - NetworkTrafficData: speed metrics, adapter info, timestamps
   - DisplayConfiguration: theme, position, update intervals
   - NetworkAdapter: connection info, status, capabilities

2. **Generate API contracts** from functional requirements:
   - NetworkMonitor library: StartMonitoring(), StopMonitoring(), GetCurrentTraffic()
   - TaskbarIntegration library: ShowDisplay(), HideDisplay(), UpdateDisplay()
   - UIComponents library: ShowDetailedStats(), ApplyTheme(), HandleInteraction()
   - Output interface definitions to `/contracts/`

3. **Generate contract tests** from contracts:
   - Test files for each library interface
   - Assert method signatures and behavior contracts
   - Tests must fail (no implementation yet)

4. **Extract test scenarios** from user stories:
   - Real-time display updates test scenario
   - Taskbar integration seamless operation scenario  
   - Modern GUI detailed statistics display scenario
   - Windows theme adaptation test scenario

5. **Update agent file incrementally** (O(1) operation):
   - Run `/scripts/powershell/update-agent-context.ps1 -AgentType cursor`
   - Add C#/.NET, Windows 11 development context
   - Include taskbar integration and network monitoring specifics
   - Keep under 150 lines for token efficiency

**Output**: data-model.md, /contracts/*, failing tests, quickstart.md, CURSOR.md

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load `/templates/tasks-template.md` as base
- Generate tasks from Phase 1 design docs (contracts, data model, quickstart)
- Each contract interface → contract test task [P]
- Each entity → model creation task [P] 
- Each user story → integration test task
- Implementation tasks to make tests pass
- Windows-specific integration tasks (taskbar, themes, system resources)

**Ordering Strategy**:
- TDD order: Tests before implementation 
- Dependency order: Models → Services → UI Components → Integration
- Mark [P] for parallel execution (independent libraries)
- Windows integration tasks after core functionality

**Estimated Output**: 28-32 numbered, ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*No constitution violations identified - design follows all constitutional principles*

## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)
- [x] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved
- [x] Complexity deviations documented (none required)

---
*Based on Constitution v2.1.1 - See `/memory/constitution.md`*
