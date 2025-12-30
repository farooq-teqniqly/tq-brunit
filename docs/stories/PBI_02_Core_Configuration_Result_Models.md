# PBI: Implement core configuration and result models

**Story ID**: PBI-02  
**Sprint**: 2 weeks  
**Estimate**: 1 day  
**Proposal Reference**: [`docs/proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md`](../proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md) - Section 5 (Contracts), Section 6 (Testing Strategy)

---

## User story

As a **developer using the library**  
I want **immutable configuration and result types**  
So that **I can safely pass options and inspect results without mutation concerns**

---

## Scope

**In scope:**

- Implement `BrunoRunOptions` as a sealed record with all properties
- Implement `BrunoRunResult` as a sealed record with all properties
- Add XML documentation comments
- Implement default values per spec
- Ensure immutability (init-only properties)
- Add `IsSuccess` computed property to `BrunoRunResult`

**Out of scope:**

- Runner implementation
- Process execution logic
- Validation logic (handled in runner)

---

## Acceptance Criteria

- [ ] `BrunoRunOptions` is a sealed record with all properties from spec
- [ ] `BrunoRunOptions` has correct default values (`BruExecutablePath = "bru"`, `Timeout = 2 minutes`, etc.)
- [ ] `BrunoRunResult` is a sealed record with all properties from spec
- [ ] `BrunoRunResult.IsSuccess` returns `true` when `ExitCode == 0`
- [ ] `BrunoRunResult.IsSuccess` returns `false` when `ExitCode != 0`
- [ ] All properties are `init`-only (immutability)
- [ ] XML documentation comments present on all public members
- [ ] Unit tests verify default values
- [ ] Unit tests verify immutability (cannot modify after initialization)
- [ ] Unit tests verify value equality (record feature)
- [ ] Unit tests verify `IsSuccess` logic for various exit codes

---

## Tasks

- Create `BrunoRunOptions.cs` with record definition
- Create `BrunoRunResult.cs` with record definition
- Add XML documentation comments
- Write unit tests for `BrunoRunOptions` (defaults, immutability, equality)
- Write unit tests for `BrunoRunResult` (IsSuccess logic, immutability, equality)
- Verify tests pass

---

## Notes

- Reference: Proposal Section 5 (Contracts - BrunoRunOptions, BrunoRunResult)
- Reference: Proposal Section 6 (Testing Strategy - Test categories 1 & 2)
- Use TDD: Write tests first, then implement
- Records provide value equality automatically - test this behavior
- **Dependencies**: PBI-01 (Scaffold)
- **Blocks**: PBI-03, PBI-04, PBI-05
