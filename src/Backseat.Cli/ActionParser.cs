using System.Globalization;
using Backseat.Core;

namespace Backseat.Cli;

public static class ActionParser
{
    public static readonly string[] ActionOptions = { "click", "token", "type", "key", "scroll", "wait" };

    public static ComputerAction Parse(CliArguments arguments)
    {
        var provided = ActionOptions.Where(option => arguments.GetValue(option) is not null).ToList();

        if (provided.Count == 0)
        {
            throw new CliUsageException("Exactly one action option is required: --click, --token, --type, --key, --scroll, or --wait.");
        }

        if (provided.Count > 1)
        {
            throw new CliUsageException($"Provide exactly one action option; got {string.Join(", ", provided.Select(option => "--" + option))}.");
        }

        return provided[0] switch
        {
            "click" => ParseClick(arguments.RequireValue("click")),
            "token" => new ClickAction(arguments.RequireValue("token")),
            "type" => new TypeTextAction(arguments.RequireValue("type")),
            "key" => new PressKeyAction(arguments.RequireValue("key")),
            "scroll" => ParseScroll(arguments),
            "wait" => new WaitAction(TimeSpan.FromMilliseconds(ParsePositiveNumber(arguments.RequireValue("wait"), "wait"))),
            _ => throw new CliUsageException("Unreachable action option."),
        };
    }

    private static ClickAction ParseClick(string value)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
            || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
        {
            throw new CliUsageException($"Option '--click' must be 'x,y' in window-local pixels, got '{value}'.");
        }

        return new ClickAction(x, y);
    }

    private static ScrollAction ParseScroll(CliArguments arguments)
    {
        var direction = arguments.RequireValue("scroll").ToLowerInvariant();
        var ticks = arguments.GetUInt("ticks") ?? 1;

        if (ticks is 0 or > int.MaxValue)
        {
            throw new CliUsageException("Option '--ticks' must be between 1 and 2147483647.");
        }

        return direction switch
        {
            "up" => new ScrollAction(ScrollAxis.Vertical, -(int)ticks),
            "down" => new ScrollAction(ScrollAxis.Vertical, (int)ticks),
            "left" => new ScrollAction(ScrollAxis.Horizontal, -(int)ticks),
            "right" => new ScrollAction(ScrollAxis.Horizontal, (int)ticks),
            _ => throw new CliUsageException($"Option '--scroll' must be up, down, left, or right; got '{direction}'."),
        };
    }

    private static double ParsePositiveNumber(string value, string option)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            throw new CliUsageException($"Option '--{option}' must be a positive number, got '{value}'.");
        }

        return parsed;
    }
}
