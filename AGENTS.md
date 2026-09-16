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
- ❌ Magic numbers or strings — except the expected code and message of an assertion, which are
  always plain literals (see Testing)
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
- A `ValidationResult` is asserted with `ShouldReport` (`NValidation/Testing/ValidationAssertions.cs`,
  namespace `NValidation.Testing`, **shipped** with the package), which states the *whole* expected
  result: `result.ShouldReport("Vin", "Vin is required.")` for one failure, and the list form for
  several. Nothing else may be present and the count is implied, so a repeated entry asks for a
  repeated failure; order is ignored. The same overloads take a `ValidationException` or a
  `{ code: [messages] }` dictionary — `ToErrorsDictionary()`, `ValidationException.Errors`, or the
  `errors` member of a problem-details response.
  ```csharp
  result.ShouldReport([
      ExpectedError.Any("FeatureIds"),                                   // the wording is not the point here
      ExpectedError.Matching("Model.Manufacturer.ContactEmail", "*email address*"), // a fragment
      new("ServiceHistory[0].Workshop", "Workshop is required.")]);
  ```
  The message is matched **exactly**, as `AwesomeAssertions`' `Be` does, so an expected message means
  what it says — a `?` is a question mark, not a wildcard. The two looser forms each say so at the call
  site: `ExpectedError.Matching(name, pattern)` takes wildcards (`*` any run, `?` one, `\*` and `\?`
  for those characters themselves), mirroring `Match`, and `ExpectedError.Any(name)` accepts any
  message. Reach for them sparingly: a pinned message is what makes the test catch a rule wired to the
  wrong code. Success is `result.Errors.Should().BeEmpty()`. A negative match
  (`Message.Should().NotContain(...)`) has no spelling here and stays on AwesomeAssertions —
  `PropertyRuleBuilderExtensionsTests.Text.cs` has the one site that needs it.
- `ShouldReport` depends on no test framework and no assertion library: it throws
  `ValidationAssertionException`, and its own matching, pairing and failure text are tested in
  `Tests/NValidation.Tests/Testing/`. Those tests pin the failure text verbatim, because an assertion
  whose message stops naming the difference is the one thing this cannot afford to regress.
- The expected code and message are written as plain string literals, never as `nameof`, a shared
  constant or a local: the literal is what a reader needs to see. Renames then cost a few more
  edits, which is the trade. A theory carries them in its `[InlineData]` rows.
- A rule that compares against "now" takes a `TestTimeProvider`
  (`new TestTimeProvider(new DateTimeOffset(...))`), never the ambient clock.
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
- The rules a test is about are declared in the test itself, on `TestValidator<T>`
  (`NValidation/Testing/TestValidator.cs`, **shipped** with the package), which re-exposes the base
  class's `protected` `Property`:
  `var validator = new TestValidator<Car>(); validator.Property(c => c.Vin).NotEmpty();`.
  Rules first, a blank line, then the payload. `new TestValidator<Car>()` answers in the built-in
  English, for a test about the message a rule renders; a test about *which* rule reported asserts
  `ValidationError.ErrorCode` through `ShouldReportErrorCode` instead. The provider is chosen at
  construction, so nothing has to be sequenced or put back. Do not add a validator class per test case — the reader
  should not have to open a second file to learn what is being validated. A named validator, declared
  `private sealed` inside the test class that needs it, is for the case where something other than the
  rule chain depends on the *type*: the container constructs it by type, or a constant or payload factory
  it declares serves several tests.
- Every test carries `[Trait(Traits.Category, Traits.UnitTests)]`, declared once on the test class (a
  partial class carries it on whichever part declares it). The trait is what lets a run be filtered by
  category; CI currently runs everything, so a missing trait costs nothing today and everything on the
  day someone adds `--filter`.
- Tests which change the ambient culture carry `[Collection(Collections.CultureSpecific)]`, because the
  culture is process-wide.

## Vocabulary
Two words, each meaning exactly one thing, everywhere in code, tests and docs:
- **`PropertyName`** — where a failure is: the dotted member path (`Model.Manufacturer.Name`,
  `ServiceHistory[1].Workshop`). It is what a failure is reported under and what a client binds to.
  `WithPropertyName` overrides it for a whole property.
- **`ErrorCode`** — which rule failed (`NotEmpty`, `GreaterThan`), and the key its message is resolved
  under. `WithErrorCode` overrides it for the one rule it follows.

Do not reintroduce "code" for a path or "message key" for a code. `ValidationError.Code` and
`ValidationMessageKeys` were the old names for these and are gone.

## Concurrency
- A validator may be registered as a singleton and validate many requests at once, so everything a run
  touches must belong to that run. `Tests/NValidation.Tests/ConcurrencyTests.cs` holds one validator and
  hammers it; each worker validates its own payload carrying its own identity, so a leak shows up as one
  worker being told about another rather than as a vague count mismatch. It runs in its own xUnit
  collection because it saturates the thread pool.
- Do not use `Barrier.SignalAndWait` inside a `Parallel.ForEachAsync` body: blocking more pool threads
  than there are cores stalls until the pool injects more, roughly one per second. Race on dedicated
  `Thread`s instead.
- The compiled-accessor caches live for the process, so a test that means to race a *cold* key needs a
  payload type declared for it alone.

## Library-specific rules
- Every shipped rule needs at least one test, including its boundary values and its null/absent case.
- Every shipped rule also needs a test asserting the error's `PropertyName` and its `ErrorCode`, with
  `result.ShouldReportErrorCode("Vin", "NotEmpty")` — one validator, one run, no message provider to swap.
  (`ErrorCodeProvider` still works, and is how to assert a code *through* the message a provider
  returns; `ShouldReportErrorCode` is the shorter route and needs no provider.)
  These live beside the rule's own tests, in
  the matching `PropertyRuleBuilderExtensionsTests.*` part. Asserting only `result.Succeeded` lets a
  rule wired to a neighbouring key pass the whole suite — a value that is too large reported as "must
  be greater than". The bar is a mutation: changing any rule's key must turn the suite red.
- A new key also needs its built-in English message: `DefaultValidationMessageProviderTests` walks every
  constant on `ValidationErrorCodes` and fails for one the provider has no text for.
- `NValidation` must not reference ASP.NET Core. Anything needing `ProblemDetails`, `ControllerBase` or
  `IExceptionHandler` belongs in `NValidation.AspNetCore`.
- Rules never reference a resource or a literal message: they report a key from `ValidationErrorCodes`,
  which the host resolves through `IValidationMessageProvider`. A new rule needs a new key plus its
  built-in English message in `DefaultValidationMessageProvider`.
- A rule's property expression must reach the property through the lambda's own parameter. `PropertyPath`
  refuses anything else, because the path it produces is both the reported property name and the key the compiled
  accessor and the reachability guard are cached under: two expressions sharing a path would share a
  delegate and validate the wrong value.
- Anything absent passes rather than throws — a null nested object, a missing collection, an absent
  value, a compared property behind an object the payload omitted. Requiring presence is always a rule
  of its own. A rule that dereferences without a guard turns a bad request into a 500.
