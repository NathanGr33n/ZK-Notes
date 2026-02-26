using FluentAssertions;
using ZKNotes.Helpers;

namespace ZKNotes.Tests.Helpers;

public class TagParserTests
{
    [Fact]
    public void ExtractTags_WithNoTags_ReturnsEmptyList()
    {
        // Arrange
        var content = "This is a note with no tags.";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractTags_WithNullContent_ReturnsEmptyList()
    {
        // Act
        var result = TagParser.ExtractTags(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractTags_WithEmptyString_ReturnsEmptyList()
    {
        // Act
        var result = TagParser.ExtractTags(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractTags_WithSingleTag_ExtractsTag()
    {
        // Arrange
        var content = "This note has #research tag.";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("research");
    }

    [Fact]
    public void ExtractTags_WithMultipleTags_ExtractsAllTags()
    {
        // Arrange
        var content = "This note has #research #idea #philosophy tags.";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().HaveCount(3)
            .And.Contain(new[] { "research", "idea", "philosophy" });
    }

    [Fact]
    public void ExtractTags_WithDuplicateTags_ReturnsDeduplicated()
    {
        // Arrange
        var content = "This note has #research and more #research content.";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("research");
    }

    [Fact]
    public void ExtractTags_WithMixedCase_NormalizesToLowercase()
    {
        // Arrange
        var content = "Tags: #Research #IDEA #PhiLoSoPhy";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().HaveCount(3)
            .And.Contain(new[] { "research", "idea", "philosophy" });
    }

    [Fact]
    public void ExtractTags_WithDuplicateMixedCase_DeduplicatesAfterNormalization()
    {
        // Arrange
        var content = "#Research #research #RESEARCH";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("research");
    }

    [Fact]
    public void ExtractTags_WithHyphens_ExtractsTag()
    {
        // Arrange
        var content = "Using #deep-learning in this note.";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("deep-learning");
    }

    [Fact]
    public void ExtractTags_WithUnderscores_ExtractsTag()
    {
        // Arrange
        var content = "Using #machine_learning in this note.";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("machine_learning");
    }

    [Fact]
    public void ExtractTags_WithNumbers_ExtractsTag()
    {
        // Arrange
        var content = "Project #phase2 and #v1_5";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().HaveCount(2)
            .And.Contain(new[] { "phase2", "v1_5" });
    }

    [Fact]
    public void ExtractTags_WithPureNumbers_IgnoresTag()
    {
        // Arrange
        var content = "This is #123 not a tag but #tag123 is.";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("tag123");
    }

    [Fact]
    public void ExtractTags_WithMarkdownHeading_IgnoresHeading()
    {
        // Arrange
        var content = @"# Heading
This is #research content.
## Subheading
More #idea text.";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().HaveCount(2)
            .And.Contain(new[] { "research", "idea" });
        result.Should().NotContain(new[] { "heading", "subheading" });
    }

    [Fact]
    public void ExtractTags_AtStartOfLine_ExtractsTag()
    {
        // Arrange
        var content = "#research at the start";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("research");
    }

    [Fact]
    public void ExtractTags_AtEndOfLine_ExtractsTag()
    {
        // Arrange
        var content = "Tag at end #research";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("research");
    }

    [Fact]
    public void ExtractTags_WithPunctuation_ExtractsTag()
    {
        // Arrange
        var content = "This is #research, and #idea.";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().HaveCount(2)
            .And.Contain(new[] { "research", "idea" });
    }

    [Fact]
    public void ExtractTags_AfterWhitespace_ExtractsTag()
    {
        // Arrange
        var content = "This ( #research ) is tagged.";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("research");
    }

    [Fact]
    public void ExtractTags_WithMultilineContent_ExtractsAllTags()
    {
        // Arrange
        var content = @"# Note Title

This is #research content.
Another line with #idea.

Final line: #philosophy";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().HaveCount(3)
            .And.Contain(new[] { "research", "idea", "philosophy" });
    }

    [Fact]
    public void ExtractTags_WithTagInMiddleOfWord_DoesNotExtract()
    {
        // Arrange
        var content = "email@domain.com has no tags";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractTags_WithTagStartingWithUnderscore_ExtractsTag()
    {
        // Arrange
        var content = "Using #_private tag.";

        // Act
        var result = TagParser.ExtractTags(content);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("_private");
    }
}
