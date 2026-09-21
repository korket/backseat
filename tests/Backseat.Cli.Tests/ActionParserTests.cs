using Backseat.Cli;
using Backseat.Core;

namespace Backseat.Cli.Tests;

public sealed class ActionParserTests
{
    private static readonly HashSet<string> ValueOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "click", "token", "type", "key", "scroll", "ticks", "wait",
    };

    private static CliArguments Parse(params string[] args) =>
        CliArguments.Parse(args, ValueOptions, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    [Fact]
    public void Parses_Coordinate_Click()
    {
        var action = ActionParser.Parse(Parse("--click", "10.5,20"));

        var click = Assert.IsType<ClickAction>(action);
        Assert.Equal(10.5, click.X);
        Assert.Equal(20, click.Y);
    }

    [Fact]
    public void Parses_Token_Click()
    {
        var action = ActionParser.Parse(Parse("--token", "s1:31"));

        var click = Assert.IsType<ClickAction>(action);
        Assert.Equal("s1:31", click.ElementToken);
    }

    [Fact]
    public void Parses_Type_And_Key()
    {
        Assert.IsType<TypeTextAction>(ActionParser.Parse(Parse("--type", "hello")));
        var key = Assert.IsType<PressKeyAction>(ActionParser.Parse(Parse("--key", "return")));
        Assert.Equal("return", key.Key);
    }

    [Fact]
    public void Parses_Scroll_Direction_And_Ticks()
    {
        var down = Assert.IsType<ScrollAction>(ActionParser.Parse(Parse("--scroll", "down", "--ticks", "3")));
        Assert.Equal(ScrollAxis.Vertical, down.Axis);
        Assert.Equal(3, down.Ticks);

        var left = Assert.IsType<ScrollAction>(ActionParser.Parse(Parse("--scroll", "left")));
        Assert.Equal(ScrollAxis.Horizontal, left.Axis);
        Assert.Equal(-1, left.Ticks);
    }

    [Fact]
    public void Parses_Wait()
    {
        var wait = Assert.IsType<WaitAction>(ActionParser.Parse(Parse("--wait", "250")));

        Assert.Equal(250, wait.Duration.TotalMilliseconds);
    }

    [Fact]
    public void Requires_Exactly_One_Action()
    {
        Assert.Throws<CliUsageException>(() => ActionParser.Parse(Parse()));
        Assert.Throws<CliUsageException>(() => ActionParser.Parse(Parse("--type", "a", "--key", "b")));
    }

    [Fact]
    public void Rejects_Malformed_Coordinates()
    {
        Assert.Throws<CliUsageException>(() => ActionParser.Parse(Parse("--click", "10")));
        Assert.Throws<CliUsageException>(() => ActionParser.Parse(Parse("--click", "a,b")));
    }

    [Fact]
    public void Rejects_Unknown_Scroll_Direction_And_Zero_Ticks()
    {
        Assert.Throws<CliUsageException>(() => ActionParser.Parse(Parse("--scroll", "sideways")));
        Assert.Throws<CliUsageException>(() => ActionParser.Parse(Parse("--scroll", "up", "--ticks", "0")));
    }

    [Fact]
    public void Rejects_Non_Positive_Wait()
    {
        Assert.Throws<CliUsageException>(() => ActionParser.Parse(Parse("--wait", "0")));
        Assert.Throws<CliUsageException>(() => ActionParser.Parse(Parse("--wait", "abc")));
    }
}
