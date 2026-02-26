using FluentAssertions;
using ZKNotes.Helpers;

namespace ZKNotes.Tests.Helpers;

public class LinkParserTests
{
    [Fact]
    public void ExtractLinks_WithNoLinks_ReturnsEmptyList()
    {
        // Arrange
        var content = "This is a note with no links.";

        // Act
        var result = LinkParser.ExtractLinks(content);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractLinks_WithNullContent_ReturnsEmptyList()
    {
        // Act
        var result = LinkParser.ExtractLinks(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractLinks_WithEmptyString_ReturnsEmptyList()
    {
        // Act
        var result = LinkParser.ExtractLinks(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractLinks_WithSingleSimpleLink_ExtractsLink()
    {
        // Arrange
        var content = "See [[ZK-0001]] for details.";

        // Act
        var result = LinkParser.ExtractLinks(content);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("ZK-0001");
    }

    [Fact]
    public void ExtractLinks_WithMultipleLinks_ExtractsAllLinks()
    {
        // Arrange
        var content = "See [[ZK-0001]] and [[ZK-0002]] for more info. Also check [[Another Note]].";

        // Act
        var result = LinkParser.ExtractLinks(content);

        // Assert
        result.Should().HaveCount(3)
            .And.ContainInOrder("ZK-0001", "ZK-0002", "Another Note");
    }

    [Fact]
    public void ExtractLinks_WithAliasedLink_ExtractsTarget()
    {
        // Arrange
        var content = "See [[ZK-0001|my reference]] for details.";

        // Act
        var result = LinkParser.ExtractLinks(content);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("ZK-0001");
    }

    [Fact]
    public void ExtractLinks_WithMultipleAliasedLinks_ExtractsAllTargets()
    {
        // Arrange
        var content = "[[ZK-0001|First]] and [[ZK-0002|Second]] notes.";

        // Act
        var result = LinkParser.ExtractLinks(content);

        // Assert
        result.Should().HaveCount(2)
            .And.ContainInOrder("ZK-0001", "ZK-0002");
    }

    [Fact]
    public void ExtractLinks_WithWhitespace_TrimsTargets()
    {
        // Arrange
        var content = "[[ ZK-0001 ]] and [[  ZK-0002  |  alias  ]]";

        // Act
        var result = LinkParser.ExtractLinks(content);

        // Assert
        result.Should().HaveCount(2)
            .And.ContainInOrder("ZK-0001", "ZK-0002");
    }

    [Fact]
    public void ExtractLinks_WithEmptyBrackets_IgnoresEmptyLinks()
    {
        // Arrange
        var content = "[[]] and [[ZK-0001]]";

        // Act
        var result = LinkParser.ExtractLinks(content);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("ZK-0001");
    }

    [Fact]
    public void ExtractLinks_WithOnlyWhitespaceBrackets_IgnoresEmptyLinks()
    {
        // Arrange
        var content = "[[   ]] and [[ZK-0001]]";

        // Act
        var result = LinkParser.ExtractLinks(content);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be("ZK-0001");
    }

    [Fact]
    public void ExtractLinks_WithMultilineContent_ExtractsAllLinks()
    {
        // Arrange
        var content = @"# Note Title

This is a paragraph with [[ZK-0001]].

Another paragraph with [[ZK-0002|reference]].";

        // Act
        var result = LinkParser.ExtractLinks(content);

        // Assert
        result.Should().HaveCount(2)
            .And.ContainInOrder("ZK-0001", "ZK-0002");
    }

    [Fact]
    public void ReplaceLinksForHtml_WithNoLinks_ReturnsOriginalContent()
    {
        // Arrange
        var content = "This is a note with no links.";

        // Act
        var result = LinkParser.ReplaceLinksForHtml(content);

        // Assert
        result.Should().Be(content);
    }

    [Fact]
    public void ReplaceLinksForHtml_WithNullContent_ReturnsEmptyString()
    {
        // Act
        var result = LinkParser.ReplaceLinksForHtml(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ReplaceLinksForHtml_WithSimpleLink_CreatesHtmlLink()
    {
        // Arrange
        var content = "See [[ZK-0001]] for details.";

        // Act
        var result = LinkParser.ReplaceLinksForHtml(content);

        // Assert
        result.Should().Contain("<a href=\"zk://note/ZK-0001\" class=\"note-link\">ZK-0001</a>");
    }

    [Fact]
    public void ReplaceLinksForHtml_WithAliasedLink_UsesAliasAsDisplay()
    {
        // Arrange
        var content = "See [[ZK-0001|My Reference]] for details.";

        // Act
        var result = LinkParser.ReplaceLinksForHtml(content);

        // Assert
        result.Should().Contain("<a href=\"zk://note/ZK-0001\" class=\"note-link\">My Reference</a>");
    }

    [Fact]
    public void ReplaceLinksForHtml_WithSpecialCharacters_EncodesCharacters()
    {
        // Arrange
        var content = "[[Note with <special> chars]]";

        // Act
        var result = LinkParser.ReplaceLinksForHtml(content);

        // Assert
        result.Should().Contain("&lt;special&gt;");
    }

    [Fact]
    public void ReplaceLinksForHtml_WithUrlEncodableCharacters_EncodesUrl()
    {
        // Arrange
        var content = "[[Note with spaces]]";

        // Act
        var result = LinkParser.ReplaceLinksForHtml(content);

        // Assert
        result.Should().Contain("zk://note/Note+with+spaces");
    }
}
