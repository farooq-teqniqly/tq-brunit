# PBI: Define runner interface and implement basic success path

**Story ID**: PBI-03  
**Status**: ✅ **COMPLETED**  
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

- [x] `IBrunoRunner` interface defined with `RunAsync` method signature
- [x] `BrunoRunner` implements `IBrunoRunner`
- [x] `BrunoRunner.RunAsync` executes `bru` CLI process successfully
- [x] `BrunoRunner.RunAsync` captures standard output correctly
- [x] `BrunoRunner.RunAsync` captures standard error correctly
- [x] `BrunoRunner.RunAsync` returns correct exit code (0 for success)
- [x] `BrunoRunner.RunAsync` sets working directory correctly
- [x] `BrunoRunner.RunAsync` passes environment variables to process
- [x] Unit tests verify successful execution path
- [x] Unit tests verify output capture (stdout/stderr)
- [x] Unit tests verify working directory is set
- [x] Unit tests verify environment variables are passed
- [x] Integration test (conditional) executes real Bruno command if available

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
- Reference: Proposal Appendix B (Process execution pattern, Bruno CLI Command Options)
- **Bruno CLI Documentation**: [https://docs.usebruno.com/bru-cli/commandOptions](https://docs.usebruno.com/bru-cli/commandOptions) - Command options reference
- Use TDD: Write tests first, then implement
- For unit tests, consider using a test executable or mocking Process (if feasible)
- **Dependencies**: PBI-01 (Scaffold) ✅, PBI-02 (Core Models) ✅
- **Blocks**: PBI-04, PBI-05

### Slice 8: Build Bruno CLI Arguments

The Bruno CLI command structure follows the pattern:

```bash
bru run [--env <name>] <target>
```

Where:

- `run` is the command
- `--env <name>` is optional (when `BrunoRunOptions.EnvironmentName` is provided)
- `<target>` is the required file or folder path (from `BrunoRunOptions.Target`)

**Reference:** [Bruno CLI Command Options Documentation](https://docs.usebruno.com/bru-cli/commandOptions)

The implementation should build these arguments correctly, escaping paths that contain spaces or special characters.

---

## Completion Notes

**Completed**: All acceptance criteria verified and met.

- ✅ `IBrunoRunner` interface defined with `RunAsync` method signature
- ✅ `BrunoRunner` class implements `IBrunoRunner` with process execution abstraction
- ✅ Process execution using `System.Diagnostics.Process` with proper async/await patterns
- ✅ Standard output and standard error capture implemented
- ✅ Exit code returned correctly in `BrunoRunResult`
- ✅ Working directory configuration supported
- ✅ Environment variable passing implemented (with null-to-empty-string conversion)
- ✅ Bruno CLI argument building using `ProcessStartInfo.ArgumentList` for cross-platform escaping
- ✅ Cancellation handling with proper process cleanup (kills process tree on cancellation)
- ✅ Comprehensive unit tests (38 tests total, all passing)
- ✅ Integration test with real Bruno CLI (conditionally skips if not available)
- ✅ XML documentation complete for all public members
- ✅ Code follows Teqniqly stack conventions
- ✅ Ready for PBI-04 (Error Handling, Validation, and Timeout Support)
