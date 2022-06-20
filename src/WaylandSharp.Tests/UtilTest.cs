using WaylandSharpGen;

namespace WaylandSharp.Tests;

public class UtilTest
{
    [Theory]
    [InlineData("snake_case", "SnakeCase")]
    [InlineData("_snake_case", "SnakeCase")]
    [InlineData("snake", "Snake")]
    public void PascalCase(string input, string expected)
    {
        var actual = input.SnakeToPascalCase();
        actual.Should().Be(expected);
    }
}