# Teqniqly.BRUnit.Testing – Sprint Stories Index

**Sprint Length**: 2 weeks  
**Target Milestone**: v0.1.0 (Core runner per spec)  
**Proposal Reference**: [`docs/proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md`](../proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md)

---

## Story List

1. ✅ **[PBI-01: Scaffold project structure and CI pipeline](PBI_01_Scaffold_Project_Structure.md)** (1 day) - **COMPLETED**
2. ✅ **[PBI-02: Implement core configuration and result models](PBI_02_Core_Configuration_Result_Models.md)** (1 day) - **COMPLETED**
3. ✅ **[PBI-03: Define runner interface and implement basic success path](PBI_03_Runner_Interface_Basic_Success.md)** (2 days) - **COMPLETED**
4. **[PBI-04: Add error handling, validation, and timeout support](PBI_04_Error_Handling_Timeout.md)** (2 days)
5. **[PBI-05: Add cross-platform executable resolution and environment name support](PBI_05_Cross_Platform_Environment_Support.md)** (1 day)
6. **[PBI-06: Add comprehensive documentation and package for NuGet](PBI_06_Documentation_NuGet_Package.md)** (1 day)

---

## Story Dependencies

```text
PBI-01 (Scaffold)
    ↓
PBI-02 (Core Models)
    ↓
PBI-03 (Basic Runner)
    ↓
PBI-04 (Error Handling)
    ↓
PBI-05 (Cross-Platform)
    ↓
PBI-06 (Documentation)
```

---

## Sprint Capacity

**Estimated Sprint Capacity**: 2 weeks (10 working days)

**Story Estimates** (relative):

- PBI-01: 1 day
- PBI-02: 1 day
- PBI-03: 2 days
- PBI-04: 2 days
- PBI-05: 1 day
- PBI-06: 1 day

**Total**: ~8 days (leaves buffer for refinement and unexpected issues)

---

## Definition of Done

Each PBI is considered done when:

- [ ] All acceptance criteria are met
- [ ] All unit tests pass (>90% code coverage target)
- [ ] Code follows Teqniqly stack conventions (see `.cursor/rules/TEQNIQLY_STACK.cursorrules`)
- [ ] No SonarAnalyzer warnings or errors
- [ ] XML documentation comments are complete
- [ ] Code has been reviewed (peer review or self-review with checklist)
- [ ] CI pipeline is green
- [ ] Changes are committed and pushed

---

## Notes for Sprint Planning

- **TDD Approach**: All stories should follow TDD (Red-Green-Refactor)
- **Test Framework**: xUnit with NSubstitute (per Teqniqly conventions)
- **Assertions**: Built-in xUnit assertions only (no FluentAssertions)
- **Integration Tests**: May require Bruno CLI to be installed - mark as conditional/skippable
- **Cross-Platform Testing**: Test on Windows first; Linux/macOS testing can be done in CI or documented as platform-specific behavior
