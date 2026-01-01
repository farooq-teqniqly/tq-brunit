# PBI: Add error handling, validation, and timeout support

**Story ID**: PBI-04  
**Status**: ✅ **COMPLETED**  
**Sprint**: 2 weeks  
**Estimate**: 2 days  
**Proposal Reference**: [`docs/proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md`](../proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md) - Section 5 (Error handling), Section 6 (Testing Strategy), Appendix B

---

## User story

As a **developer using the library**  
I want **robust error handling and timeout support**  
So that **my tests don't hang indefinitely and I get clear error messages**

---

## Scope

**In scope:**

- Add input validation (null checks, required fields)
- Implement timeout handling using `Process.WaitForExitAsync` with timeout
- Handle timeout exceptions (kill process, throw `TimeoutException`)
- Handle process start failures
- Handle missing executable scenarios
- Return appropriate `BrunoRunResult` for Bruno CLI failures (non-zero exit codes)
- Throw appropriate exceptions for system-level failures

**Out of scope:**

- Cross-platform executable resolution (next story)
- Advanced error recovery
- Retry logic

---

## Acceptance Criteria

- [x] `RunAsync` throws `ArgumentNullException` when options is null
- [x] `RunAsync` throws `ArgumentException` when `Target` is empty or whitespace
- [x] `RunAsync` throws `TimeoutException` when execution exceeds timeout
- [x] `RunAsync` kills process when timeout occurs
- [x] `RunAsync` returns `BrunoRunResult` with non-zero exit code when Bruno fails
- [x] `RunAsync` captures error output in `StandardError` when Bruno fails
- [x] `RunAsync` throws `InvalidOperationException` when process cannot be started
- [x] Unit tests verify all exception scenarios
- [x] Unit tests verify timeout handling
- [x] Unit tests verify error result capture
- [x] Integration test verifies timeout behavior (if feasible)

---

## Tasks

- [x] Add validation logic at start of `RunAsync`
- [x] Implement timeout handling with `Task.WhenAny` and `Task.Delay`
- [x] Add process kill logic on timeout
- [x] Add exception handling for process start failures
- [x] Write unit tests for all error scenarios
- [x] Write unit tests for timeout scenarios
- [x] Update XML documentation with exception details

---

## Notes

- Reference: Proposal Section 5 (Error handling)
- Reference: Proposal Section 6 (Testing Strategy - Test category 4)
- Reference: Proposal Appendix B (Process execution pattern - timeout handling)
- Use TDD: Write failing tests first, then implement
- **Dependencies**: PBI-01 (Scaffold), PBI-02 (Core Models), PBI-03 (Basic Runner)
- **Blocks**: PBI-05, PBI-06
