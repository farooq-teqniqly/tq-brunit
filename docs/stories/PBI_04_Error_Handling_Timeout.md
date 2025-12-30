# PBI: Add error handling, validation, and timeout support

**Story ID**: PBI-04  
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

- [ ] `RunAsync` throws `ArgumentNullException` when options is null
- [ ] `RunAsync` throws `ArgumentException` when `Target` is empty or whitespace
- [ ] `RunAsync` throws `TimeoutException` when execution exceeds timeout
- [ ] `RunAsync` kills process when timeout occurs
- [ ] `RunAsync` returns `BrunoRunResult` with non-zero exit code when Bruno fails
- [ ] `RunAsync` captures error output in `StandardError` when Bruno fails
- [ ] `RunAsync` throws `InvalidOperationException` when process cannot be started
- [ ] Unit tests verify all exception scenarios
- [ ] Unit tests verify timeout handling
- [ ] Unit tests verify error result capture
- [ ] Integration test verifies timeout behavior (if feasible)

---

## Tasks

- Add validation logic at start of `RunAsync`
- Implement timeout handling with `Task.WhenAny` and `Task.Delay`
- Add process kill logic on timeout
- Add exception handling for process start failures
- Write unit tests for all error scenarios
- Write unit tests for timeout scenarios
- Update XML documentation with exception details

---

## Notes

- Reference: Proposal Section 5 (Error handling)
- Reference: Proposal Section 6 (Testing Strategy - Test category 4)
- Reference: Proposal Appendix B (Process execution pattern - timeout handling)
- Use TDD: Write failing tests first, then implement
- **Dependencies**: PBI-01 (Scaffold), PBI-02 (Core Models), PBI-03 (Basic Runner)
- **Blocks**: PBI-05, PBI-06
