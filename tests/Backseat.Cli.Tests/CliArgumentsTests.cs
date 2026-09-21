using Backseat.Cli;

namespace Backseat.Cli.Tests;

public sealed class CliArgumentsTests
{
    private static readonly HashSet<string> Values = new(StringComparer.OrdinalIgnoreCase) { "pid", "window" };
    private static readonly HashSet<string> Flags = new(StringComparer.OrdinalIgnoreCase) { "json" };

    [Fact]
    public void Parses_Values_And_Flags()
    {
        var arguments = CliArguments.Parse(new[] { "--pid", "42", "--json" }, Values, Flags);

        Assert.Equal(42u, arguments.RequireUInt("pid"));
        Assert.True(arguments.HasFlag("json"));
        Assert.Null(arguments.GetUInt("window"));
    }

    [Fact]
    public void Unknown_Option_Is_A_Usage_Error()
    {
        Assert.Throws<CliUsageException>(() => CliArguments.Parse(new[] { "--nope" }, Values, Flags));
    }

    [Fact]
    public void Missing_Value_Is_A_Usage_Error()
    {
        Assert.Throws<CliUsageException>(() => CliArguments.Parse(new[] { "--pid" }, Values, Flags));
    }

    [Fact]
    public void Duplicate_Option_Is_A_Usage_Error()
    {
        Assert.Throws<CliUsageException>(() => CliArguments.Parse(new[] { "--pid", "1", "--pid", "2" }, Values, Flags));
    }

    [Fact]
    public void Positional_Argument_Is_A_Usage_Error()
    {
        Assert.Throws<CliUsageException>(() => CliArguments.Parse(new[] { "42" }, Values, Flags));
    }

    [Fact]
    public void RequireValue_Is_A_Usage_Error_When_Absent()
    {
        var arguments = CliArguments.Parse(Array.Empty<string>(), Values, Flags);

        Assert.Throws<CliUsageException>(() => arguments.RequireValue("pid"));
    }

    [Fact]
    public void RequireUInt_Rejects_Non_Numeric_Values()
    {
        var arguments = CliArguments.Parse(new[] { "--pid", "abc" }, Values, Flags);

        Assert.Throws<CliUsageException>(() => arguments.RequireUInt("pid"));
    }
}
