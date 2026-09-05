# AGENTS.MD

> **Agent Configuration and Capabilities**
>
> This document defines how AI coding agents should interact with this repository,
> including capabilities, constraints, and context needed for effective assistance.

## Code Quality Standards

### Must Have
- ✅ Useful XML documentation on all public APIs
- ✅ Unit tests for all new functionality
- ✅ Nullable reference type annotations
- ✅ Async/await patterns for I/O operations
- ✅ Proper exception handling
- ✅ Follow existing code style and conventions
- ✅ Respect existing .editorconfig rules

### Should Have
- ✅ Performance tests for critical paths
- ✅ Example code in documentation
- ✅ Integration tests for complex scenarios
- ✅ Benchmark tests for performance-critical code
- ✅ Clear commit messages

### Should Avoid
- ❌ Synchronous I/O in async methods
- ❌ Catching and swallowing exceptions
- ❌ Large methods (>50 lines)
- ❌ Complex nested conditionals
- ❌ Magic numbers or strings
- ❌ Unused code
- ❌ Unused non-public code only used by unit tests
- ❌ Unused using directives

## API Design Guidelines
- Follow .NET API design guidelines for consistency with the ecosystem.
- Don't overengineer APIs; keep them simple and focused.
- Don't introduce unnecessary interface abstractions; prefer concrete types unless specific extensibility is required that is preferred through an interface.
- Use immutable types where possible to enhance thread safety and predictability.
- Allow mutable types when necessary for performance or usability.
- Make APIs hard to misuse: validate inputs early, use strong types.
- Prefer method overloads over optional parameters for binary compatibility.
- Use `params ReadOnlySpan<T>` for variadic methods (C# 13+) when targeting modern runtimes.
- Consider adding `Try*` pattern methods (returning `bool`) alongside throwing versions.
- Mark obsolete APIs with `[Obsolete("message", error: false)]` before removal.

## Dependency Changes
- Agents must ask for confirmation before creating a new dependency inside the project.
- Agents must ask for confirmation before automatically adding a new project reference.
- Agents must ask for confirmation before automatically installing a new NuGet package.

## Performance Considerations
- Ensure that the code is optimized for performance without sacrificing readability.
- Ensure that the code minimizes GC allocations where possible.
    - Use `Span<T>`/`ReadOnlySpan<T>` where appropriate to reduce memory allocations.
    - Prefer `StringBuilder` for string concatenation in loops.
    - Use `ArrayPool<T>` for temporary arrays that would otherwise cause allocations.
- Ensure generated code is AOT-compatible and trimmer-friendly.
    - Avoid reflection where possible; prefer source generators.
- Use `sealed` on classes that are not designed for inheritance to enable devirtualization.
- Prefer `ReadOnlySpan<char>` over `string` for parsing and substring operations.

## Testing
- Tests are located in `{ProjectName}.Tests` projects.
- The naming convention for unit tests is `{ClassName}.Tests`.
- Use xUnit as test framework.
- Use AwesomeAssertions for asserts.
- Use Moq, AutoMocker to setup and verify mocks (if applicable).
- All unit tests must follow the Arrange-Act-Assert (AAA) pattern.
- Separate AAA sections with blank lines.
- Use comments to separate each section:
    - // Arrange
    - // Act
    - // Assert
- Use Arrange section to declare variables and setup mocks.
- Don't write // Arrange if there is nothing to arrange.
- Each test should verify one behavior.
- Prefer descriptive test names in the form: `<Method>_<Scenario>_<Result>`.
- Test both success and failure scenarios.
- Include edge cases: null inputs, empty collections, boundary values, and error conditions.
- Prioritize testing complex logic, error handling, and edge cases over trivial code.
- Every test carries `[Trait(Traits.Category, Traits.UnitTests)]`. A test without a category trait is
  filtered out by the build and silently never runs.
- Tests which change the ambient culture carry `[Collection(Collections.CultureSpecific)]`, because the
  culture is process-wide.

## Library-specific rules
- Every shipped rule needs at least one test, including its boundary values and its null/absent case.
- Every shipped rule also needs a test in `RuleMessageKeyTests` asserting the error's `Code` and its
  message key, resolved through `MessageKeyProvider`. Asserting only `result.Succeeded` lets a rule
  wired to a neighbouring key pass the whole suite — a value that is too large reported as "must be
  greater than". The bar is a mutation: changing any rule's key must turn the suite red.
- `NValidation` must not reference ASP.NET Core. Anything needing `ProblemDetails`, `ControllerBase` or
  `IExceptionHandler` belongs in `NValidation.AspNetCore`.
- Rules never reference a resource or a literal message: they report a key from `ValidationMessageKeys`,
  which the host resolves through `IValidationMessageProvider`. A new rule needs a new key plus its
  built-in English message in `DefaultValidationMessageProvider`.
