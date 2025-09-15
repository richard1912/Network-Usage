# Tasks: Windows 11 Taskbar Network Traffic Monitor

**Input**: Design documents from `specs/001-build-an-application/`
**Prerequisites**: plan.md (✓), research.md (✓), data-model.md (✓), contracts/ (✓), quickstart.md (✓)

## Execution Flow (main)
```
1. Load plan.md from feature directory
   → Extract: C# .NET 8, WPF + System.Windows.Forms, single project structure
2. Load optional design documents:
   → data-model.md: Extract 3 entities, 2 value objects → model tasks
   → contracts/: 3 files → contract test tasks
   → research.md: Extract decisions → setup tasks
   → quickstart.md: 5 scenarios → integration test tasks
3. Generate tasks by category:
   → Setup: project init, dependencies, linting
   → Tests: contract tests, integration tests (TDD enforced)
   → Core: models, services, libraries
   → Integration: system tray, theme handling, Windows APIs
   → Polish: unit tests, performance validation, documentation
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Tests before implementation (TDD)
5. Number tasks sequentially (T001-T030)
6. SUCCESS: 30 tasks ready for execution
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- All file paths are absolute for Windows environment

## Path Conventions
- **Single project structure** (per plan.md): `src/`, `tests/` at repository root
- **Project root**: `C:\Users\Richard\Documents\Network-Usage\`

## Phase 3.1: Setup
- [ ] **T001** Create C# .NET 8 WPF project structure with src/ and tests/ directories
- [ ] **T002** Initialize .NET 8 project with WPF, System.Windows.Forms, and System.Net.NetworkInformation dependencies
- [ ] **T003** [P] Configure EditorConfig, .gitignore, and code formatting tools for C# development
- [ ] **T004** [P] Setup MSTest project structure in `tests/` with contract/, integration/, and unit/ subdirectories

## Phase 3.2: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3
**CRITICAL: These tests MUST be written and MUST FAIL before ANY implementation**

### Contract Tests (Based on contracts/)
- [ ] **T005** [P] Contract test INetworkMonitor interface in `tests/contract/NetworkMonitorContractTests.cs`
- [ ] **T006** [P] Contract test ITaskbarIntegration interface in `tests/contract/TaskbarIntegrationContractTests.cs`
- [ ] **T007** [P] Contract test IUIComponents interface in `tests/contract/UIComponentsContractTests.cs`

### Integration Tests (Based on quickstart.md scenarios)
- [ ] **T008** [P] Integration test real-time display updates scenario in `tests/integration/RealTimeDisplayTests.cs`
- [ ] **T009** [P] Integration test taskbar integration seamless operation in `tests/integration/TaskbarIntegrationTests.cs`
- [ ] **T010** [P] Integration test modern GUI detailed statistics in `tests/integration/DetailedStatsTests.cs`
- [ ] **T011** [P] Integration test Windows theme adaptation in `tests/integration/ThemeAdaptationTests.cs`
- [ ] **T012** [P] Integration test network adapter changes handling in `tests/integration/AdapterChangesTests.cs`

## Phase 3.3: Core Implementation (ONLY after tests are failing)

### Data Models (Based on data-model.md entities)
- [ ] **T013** [P] NetworkTrafficData model class in `src/models/NetworkTrafficData.cs`
- [ ] **T014** [P] DisplayConfiguration model class in `src/models/DisplayConfiguration.cs`
- [ ] **T015** [P] NetworkAdapter model class in `src/models/NetworkAdapter.cs`
- [ ] **T016** [P] SpeedReading value object in `src/models/SpeedReading.cs`
- [ ] **T017** [P] WindowsTheme and SpeedUnit enumerations in `src/models/Enums.cs`

### Library Implementations (Based on contracts and architecture)
- [ ] **T018** [P] NetworkMonitorService implementing INetworkMonitor in `src/lib/NetworkMonitorService.cs`
- [ ] **T019** [P] TaskbarIntegrationService implementing ITaskbarIntegration in `src/lib/TaskbarIntegrationService.cs`
- [ ] **T020** [P] UIComponentsService implementing IUIComponents in `src/lib/UIComponentsService.cs`

### CLI Interfaces (Constitutional requirement - each library needs CLI)
- [ ] **T021** [P] NetworkMonitor CLI commands in `src/cli/NetworkMonitorCLI.cs`
- [ ] **T022** [P] TaskbarIntegration CLI commands in `src/cli/TaskbarCLI.cs`
- [ ] **T023** [P] UIComponents CLI commands in `src/cli/UIComponentsCLI.cs`

### Main Application
- [ ] **T024** Main WPF application entry point in `src/MainWindow.xaml` and `src/MainWindow.xaml.cs`
- [ ] **T025** Application startup and dependency injection configuration in `src/App.xaml.cs`

## Phase 3.4: Integration

### Windows-Specific Integration
- [ ] **T026** System tray NotifyIcon integration with Windows 11 theme support
- [ ] **T027** Windows theme detection and automatic switching implementation
- [ ] **T028** Network adapter monitoring with System.Net.NetworkInformation integration
- [ ] **T029** Performance optimization to meet <1% CPU and <50MB RAM constraints

## Phase 3.5: Polish
- [ ] **T030** [P] Unit tests for validation logic in `tests/unit/ValidationTests.cs`
- [ ] **T031** Performance validation tests to verify <100ms response times and resource usage
- [ ] **T032** [P] Update repository README.md with installation and usage instructions
- [ ] **T033** Run complete manual testing using quickstart.md scenarios
- [ ] **T034** Auto-start with Windows implementation and installer creation

## Dependencies
- **Setup (T001-T004)** must complete before all other tasks
- **Tests (T005-T012)** must complete and FAIL before implementation (T013-T025)
- **Models (T013-T017)** must complete before services (T018-T020)
- **Services (T018-T020)** must complete before main application (T024-T025)
- **Core implementation (T013-T025)** must complete before integration (T026-T029)
- **Integration (T026-T029)** must complete before polish (T030-T034)

## Parallel Execution Examples

### Phase 3.2 - All Contract Tests (Launch Together):
```bash
# These can run simultaneously - different test files, no dependencies
Task: "Contract test INetworkMonitor interface in tests/contract/NetworkMonitorContractTests.cs"
Task: "Contract test ITaskbarIntegration interface in tests/contract/TaskbarIntegrationContractTests.cs"  
Task: "Contract test IUIComponents interface in tests/contract/UIComponentsContractTests.cs"
```

### Phase 3.2 - All Integration Tests (Launch Together):
```bash
# These can run simultaneously - different test files, independent scenarios
Task: "Integration test real-time display updates in tests/integration/RealTimeDisplayTests.cs"
Task: "Integration test taskbar integration in tests/integration/TaskbarIntegrationTests.cs"
Task: "Integration test modern GUI statistics in tests/integration/DetailedStatsTests.cs"
Task: "Integration test theme adaptation in tests/integration/ThemeAdaptationTests.cs"
Task: "Integration test adapter changes in tests/integration/AdapterChangesTests.cs"
```

### Phase 3.3 - All Data Models (Launch Together):
```bash
# These can run simultaneously - different model files, no interdependencies
Task: "NetworkTrafficData model class in src/models/NetworkTrafficData.cs"
Task: "DisplayConfiguration model class in src/models/DisplayConfiguration.cs"
Task: "NetworkAdapter model class in src/models/NetworkAdapter.cs"
Task: "SpeedReading value object in src/models/SpeedReading.cs"
Task: "WindowsTheme and SpeedUnit enumerations in src/models/Enums.cs"
```

### Phase 3.3 - All Library Implementations (Launch Together):
```bash
# These can run simultaneously - different service files, implementing separate contracts
Task: "NetworkMonitorService implementing INetworkMonitor in src/lib/NetworkMonitorService.cs"
Task: "TaskbarIntegrationService implementing ITaskbarIntegration in src/lib/TaskbarIntegrationService.cs"
Task: "UIComponentsService implementing IUIComponents in src/lib/UIComponentsService.cs"
```

## Notes
- **[P] tasks** = different files, no dependencies, can run in parallel
- **Verify tests fail** before implementing - strict TDD enforcement
- **Commit after each task** for proper version control
- **Windows 11 specific** - ensure all system integration works on target platform
- **Performance critical** - monitor CPU and memory usage during development
- **Theme integration** - test with Windows light/dark/high contrast themes

## Task Generation Rules Applied
*Results from main() execution*

1. **From Contracts (3 files)**:
   - INetworkMonitor.cs → T005 contract test
   - ITaskbarIntegration.cs → T006 contract test  
   - IUIComponents.cs → T007 contract test
   
2. **From Data Model (3 entities + 2 value objects)**:
   - NetworkTrafficData → T013 model task
   - DisplayConfiguration → T014 model task
   - NetworkAdapter → T015 model task
   - SpeedReading → T016 value object task
   - Enums → T017 enumeration task
   
3. **From Quickstart Scenarios (5 scenarios)**:
   - Real-time display updates → T008 integration test
   - Taskbar integration → T009 integration test
   - Modern GUI statistics → T010 integration test  
   - Theme adaptation → T011 integration test
   - Adapter changes → T012 integration test

4. **From Architecture (3 libraries)**:
   - NetworkMonitor → T018 service + T021 CLI
   - TaskbarIntegration → T019 service + T022 CLI
   - UIComponents → T020 service + T023 CLI

## Validation Checklist
*GATE: Checked before task execution*

- [x] All contracts have corresponding tests (T005-T007)
- [x] All entities have model tasks (T013-T017)
- [x] All tests come before implementation (T005-T012 before T013+)
- [x] Parallel tasks truly independent (different files, marked [P])
- [x] Each task specifies exact file path
- [x] No task modifies same file as another [P] task
- [x] TDD ordering enforced (tests must fail before implementation)
- [x] Constitutional requirements met (library-first, CLI interfaces, TDD)

**Ready for execution**: All 34 tasks generated and validated ✓
