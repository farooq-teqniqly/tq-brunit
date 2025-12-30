# PBI: Define runner interface and implement basic success path

**Story ID**: PBI-03  
**Sprint**: 2 weeks  
**Estimate**: 2 days  
**Proposal Reference**: [`docs/proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md`](../proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md) - Section 5 (Contracts), Section 6 (Testing Strategy), Appendix B

---

## User story

As a **developer using the library**  
I want **to execute Bruno CLI commands and get results**  
So that **I can run contract tests programmatically**

---

## Scope

**In scope:**

- Define `IBrunoRunner` interface with `RunAsync` method
- Implement `BrunoRunner` class implementing `IBrunoRunner`
- Implement basic process execution (success path only)
- Capture stdout and stderr
- Return exit code
- Handle working directory configuration
- Pass environment variables to process

**Out of scope:**

- Timeout handling (next story)
- Error handling for edge cases (next story)
- Cross-platform executable resolution (next story)
- Validation of options (can be basic for now)

---

## Acceptance Criteria

- [ ] `IBrunoRunner` interface defined with `RunAsync` method signature
- [ ] `BrunoRunner` implements `IBrunoRunner`
- [ ] `BrunoRunner.RunAsync` executes `bru` CLI process successfully
- [ ] `BrunoRunner.RunAsync` captures standard output correctly
- [ ] `BrunoRunner.RunAsync` captures standard error correctly
- [ ] `BrunoRunner.RunAsync` returns correct exit code (0 for success)
- [ ] `BrunoRunner.RunAsync` sets working directory correctly
- [ ] `BrunoRunner.RunAsync` passes environment variables to process
- [ ] Unit tests verify successful execution path
- [ ] Unit tests verify output capture (stdout/stderr)
- [ ] Unit tests verify working directory is set
- [ ] Unit tests verify environment variables are passed
- [ ] Integration test (conditional) executes real Bruno command if available

---

## Tasks

- Create `IBrunoRunner.cs` interface
- Create `BrunoRunner.cs` class
- Implement basic `Process.Start` logic with output redirection
- Add working directory configuration
- Add environment variable passing
- Write unit tests using NSubstitute or real process execution
- Write integration test (skip if Bruno not installed)

---

## Notes

- Reference: Proposal Section 5 (Contracts - IBrunoRunner, BrunoRunner)
- Reference: Proposal Section 6 (Testing Strategy - Test category 3)
- Reference: Proposal Appendix B (Process execution pattern)
- Use TDD: Write tests first, then implement
- For unit tests, consider using a test executable or mocking Process (if feasible)
- **Dependencies**: PBI-01 (Scaffold), PBI-02 (Core Models)
- **Blocks**: PBI-04, PBI-05
