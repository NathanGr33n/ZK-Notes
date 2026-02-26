# ZKNotes.Tests

Unit and integration tests for the ZK-Notes application.

## Test Structure

```
tests/ZKNotes.Tests/
  Helpers/         — Tests for LinkParser, TagParser, etc.
  Services/        — Tests for StorageService, SearchService, etc.
```

## Running Tests

From the repository root:

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test project
dotnet test tests/ZKNotes.Tests/ZKNotes.Tests.csproj

# Run tests with coverage (requires coverlet)
dotnet test /p:CollectCoverage=true
```

## Test Categories

### Helper Tests
- **LinkParserTests**: Tests for `[[wiki-link]]` extraction and HTML conversion
  - Simple links, aliased links, special characters, whitespace handling
  - 15 test cases covering edge cases and validation

- **TagParserTests**: Tests for `#hashtag` extraction
  - Tag normalization, deduplication, special characters
  - Markdown heading exclusion, number handling
  - 20 test cases covering various scenarios

### Service Tests
- **StorageServiceTests**: Tests for note persistence and file I/O
  - CRUD operations, ID generation, YAML frontmatter
  - Concurrency, error handling, malformed file handling
  - 24 test cases with temporary test directories

## Test Frameworks

- **xUnit**: Testing framework
- **FluentAssertions**: Fluent assertion library for readable test assertions
- **coverlet.collector**: Code coverage collection

## Writing New Tests

Follow the existing patterns:

1. **Arrange**: Set up test data and dependencies
2. **Act**: Execute the code under test
3. **Assert**: Verify the results using FluentAssertions

Example:

```csharp
[Fact]
public void MyTest_WithInput_ReturnsExpectedOutput()
{
    // Arrange
    var input = "test input";
    
    // Act
    var result = MyFunction(input);
    
    // Assert
    result.Should().Be("expected output");
}
```

### Test Naming Convention

`MethodName_Scenario_ExpectedBehavior`

Examples:
- `ExtractLinks_WithNoLinks_ReturnsEmptyList`
- `SaveNoteAsync_WithValidNote_SavesFile`
- `GenerateNextIdAsync_MultipleCalls_ReturnsIncrementingIds`

## Test Coverage Goals

Current coverage:
- **LinkParser**: 100% (all public methods)
- **TagParser**: 100% (all public methods)
- **StorageService**: ~95% (core functionality)

Target: 80%+ coverage for all services and helpers.

## Continuous Integration

Tests run automatically on:
- Pull requests
- Commits to main branch
- Manual workflow triggers

(CI/CD setup coming soon)
