# PBI: Scaffold project structure and CI pipeline

**Story ID**: PBI-01  
**Status**: ✅ **COMPLETED**  
**Sprint**: 2 weeks  
**Estimate**: 1 day  
**Proposal Reference**: [`docs/proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md`](../proposals/BRUNIT_TESTING_CORE_TECHNICAL_PROPOSAL.md) - Section 7 (Phase 0: Scaffolding)

---

## User story

As a **developer**  
I want **a properly configured project structure with CI/CD**  
So that **I can build, test, and package the library reliably**

---

## Scope

**In scope:**

- Create `Teqniqly.BRUnit.Testing` library project targeting .NET 10
- Create `Teqniqly.BRUnit.Testing.Tests` test project with xUnit
- Configure project properties (nullable, implicit usings, analysis rules)
- Set up solution structure
- Configure CI build pipeline (build + test)
- Add NSubstitute to test project
- Ensure zero test framework dependencies in core library

**Out of scope:**

- Implementation of any business logic
- NuGet package publishing (handled in later story)

---

## Acceptance Criteria

- [x] `Teqniqly.BRUnit.Testing` project exists and targets .NET 10
- [x] `Teqniqly.BRUnit.Testing.Tests` project exists with xUnit and NSubstitute
- [x] Solution builds successfully with no warnings
- [x] Test project runs successfully (even with no tests)
- [x] CI pipeline builds and runs tests
- [x] Core library has zero dependencies on test frameworks
- [x] Project properties match Teqniqly stack conventions (nullable enabled, implicit usings, etc.)
- [x] Code analysis rules configured (SonarAnalyzer per Directory.Build.props)

---

## Tasks

- Create library project with .NET 10 target
- Create test project with xUnit and NSubstitute references
- Configure Directory.Build.props inheritance
- Set up solution file structure
- Add CI workflow (GitHub Actions or Azure DevOps)
- Verify build and test execution in CI

---

## Notes

- Reference: Proposal Section 7 (Phase 0: Scaffolding)
- Reference: Proposal Appendix A (Project Structure)
- Follow `.cursor/rules/TEQNIQLY_STACK.cursorrules` for project conventions
- **Dependencies**: None (first story)
- **Blocks**: PBI-02, PBI-03, PBI-04, PBI-05, PBI-06

---

## Completion Notes

**Completed**: All acceptance criteria verified and met.

- ✅ Project structure created and configured
- ✅ CI pipeline verified and running successfully
- ✅ All tests passing
- ✅ Build succeeds with zero warnings
- ✅ Ready for PBI-02 (Core Models implementation)

**Next Steps**: Remove placeholder `Calculator.cs` and `CalculatorTests.cs` files when starting PBI-02.
