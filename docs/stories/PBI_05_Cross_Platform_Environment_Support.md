# PBI: Add cross-platform executable resolution and environment name support

**Story ID**: PBI-05  
**Status**: ✅ **COMPLETED**  
**Sprint**: 2 weeks  
**Estimate**: 1 day  
**Proposal Reference**: [`docs/proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md`](../proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md) - Section 5 (BrunoRunOptions), Section 6 (Testing Strategy), Appendix B

---

## User story

As a **developer using the library**  
I want **cross-platform executable resolution and environment name support**  
So that **the library works on Windows, Linux, and macOS, and I can specify Bruno environments**

---

## Scope

**In scope:**

- Implement executable path resolution (handle `.exe` on Windows)
- Add support for `EnvironmentName` option (Bruno's `--env` flag)
- Build correct command-line arguments for Bruno CLI
- Handle both file and folder targets
- Test cross-platform behavior (or document platform differences)

**Out of scope:**

- Custom Bruno CLI arguments (per proposal Q4)
- Bruno collection parsing

---

## Acceptance Criteria

- [x] Executable path resolution adds `.exe` extension on Windows when not present
- [x] Executable path resolution preserves explicit paths (with separators)
- [x] `EnvironmentName` is passed to Bruno CLI as `--env <name>` argument
- [x] `Target` is passed correctly to Bruno CLI
- [x] Both file targets (`.bru` files) and folder targets work
- [x] Unit tests verify Windows executable resolution
- [x] Unit tests verify Linux/macOS executable resolution (or skip on Windows)
- [x] Unit tests verify environment name argument building
- [x] Unit tests verify target argument building
- [x] Integration test verifies environment name usage (if Bruno available)

---

## Tasks

- [x] Implement `ResolveExecutablePath` method with Windows `.exe` handling
- [x] Implement `BuildArguments` method to construct Bruno CLI arguments (using ArgumentList)
- [x] Add `--env` flag support when `EnvironmentName` is provided
- [x] Add target argument handling
- [x] Write unit tests for executable resolution
- [x] Write unit tests for argument building
- [x] Test on multiple platforms (or document platform-specific behavior)

---

## Notes

- Reference: Proposal Section 5 (BrunoRunOptions - EnvironmentName)
- Reference: Proposal Section 6 (Testing Strategy - Test category 5)
- Reference: Proposal Appendix B (Executable path resolution)
- Use TDD: Write tests first
- Consider using `OperatingSystem.IsWindows()` for platform detection
- **Dependencies**: PBI-01 (Scaffold), PBI-02 (Core Models), PBI-03 (Basic Runner), PBI-04 (Error Handling)
- **Blocks**: PBI-06
