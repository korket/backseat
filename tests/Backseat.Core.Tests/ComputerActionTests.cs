using Backseat.Core;

namespace Backseat.Core.Tests;

public sealed class ComputerActionTests
{
    [Fact]
    public void ClickAction_Accepts_Coordinates()
    {
        var click = new ClickAction(10.5, 20.25);

        Assert.Equal(10.5, click.X);
        Assert.Equal(20.25, click.Y);
        Assert.Null(click.ElementToken);
    }

    [Fact]
    public void ClickAction_Accepts_ElementToken()
    {
        var click = new ClickAction("s00000003:31");

        Assert.Equal("s00000003:31", click.ElementToken);
        Assert.Null(click.X);
    }

    [Fact]
    public void ClickAction_Requires_BackgroundSafe_By_Default()
    {
        var click = new ClickAction(1, 2);

        Assert.True(click.RequireBackgroundSafe);
    }

    [Fact]
    public void ClickAction_Rejects_Empty_ElementToken()
    {
        Assert.Throws<ArgumentException>(() => new ClickAction(" "));
    }

    [Fact]
    public void TypeTextAction_Rejects_Empty_Text()
    {
        Assert.Throws<ArgumentException>(() => new TypeTextAction(string.Empty));
    }

    [Fact]
    public void PressKeyAction_Rejects_Empty_Key()
    {
        Assert.Throws<ArgumentException>(() => new PressKeyAction(" "));
    }

    [Fact]
    public void WaitAction_Rejects_NonPositive_Duration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WaitAction(TimeSpan.Zero));
    }

    [Fact]
    public void ScrollAction_Rejects_Zero_Ticks()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScrollAction(ScrollAxis.Vertical, 0));
    }

    [Fact]
    public void ScrollAction_Preserves_Axis_And_Direction()
    {
        var up = new ScrollAction(ScrollAxis.Vertical, -2);
        var right = new ScrollAction(ScrollAxis.Horizontal, 3);

        Assert.False(up.IsForward);
        Assert.Equal(2, Math.Abs(up.Ticks));
        Assert.Equal(ScrollAxis.Horizontal, right.Axis);
        Assert.True(right.IsForward);
    }

    [Fact]
    public void Actions_Are_Polymorphic()
    {
        ComputerAction action = new PressKeyAction("return");

        Assert.IsType<PressKeyAction>(action);
    }
}
