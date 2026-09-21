using Backseat.Core;

namespace Backseat.Core.Tests;

public sealed class ActionReceiptTests
{
    [Fact]
    public void UnknownEvidence_IsNull_RatherThanFalse()
    {
        var receipt = new ActionReceipt { Effect = ActionEffect.Confirmed };

        Assert.Null(receipt.ForegroundChanged);
        Assert.Null(receipt.CursorMoved);
        Assert.Equal(ActionDelivery.Unknown, receipt.Delivery);
    }

    [Fact]
    public void ConfirmsBackgroundSafe_IsTrue_For_ConfirmedBackgroundDelivery()
    {
        var receipt = new ActionReceipt
        {
            Effect = ActionEffect.Confirmed,
            Delivery = ActionDelivery.Background,
            DeliveryRoute = "accessibility",
        };

        Assert.True(receipt.ConfirmsBackgroundSafe);
    }

    [Fact]
    public void ConfirmsBackgroundSafe_IsFalse_When_DeliveryIsUnknown()
    {
        var receipt = new ActionReceipt { Effect = ActionEffect.Confirmed };

        Assert.False(receipt.ConfirmsBackgroundSafe);
    }

    [Fact]
    public void ConfirmsBackgroundSafe_IsFalse_When_EffectIsUnverifiable()
    {
        var receipt = new ActionReceipt
        {
            Effect = ActionEffect.Unverifiable,
            Delivery = ActionDelivery.Background,
        };

        Assert.False(receipt.ConfirmsBackgroundSafe);
    }

    [Fact]
    public void ConfirmsBackgroundSafe_IsFalse_For_ForegroundDelivery()
    {
        var receipt = new ActionReceipt
        {
            Effect = ActionEffect.Confirmed,
            Delivery = ActionDelivery.Foreground,
        };

        Assert.False(receipt.ConfirmsBackgroundSafe);
    }

    [Fact]
    public void SuggestedEscalation_DoesNot_ImplySuccess()
    {
        var receipt = new ActionReceipt
        {
            Effect = ActionEffect.Unverifiable,
            SuggestedEscalation = ActionDelivery.Foreground,
        };

        Assert.False(receipt.ConfirmsBackgroundSafe);
        Assert.Null(receipt.Error);
    }

    [Fact]
    public void Warnings_And_Error_Are_Preserved()
    {
        var receipt = new ActionReceipt
        {
            Effect = ActionEffect.Failed,
            Error = "background_unavailable",
            Warnings = new[] { "cursor position unknown", "focus not verified" },
        };

        Assert.Equal("background_unavailable", receipt.Error);
        Assert.Equal(2, receipt.Warnings.Count);
        Assert.Contains("cursor position unknown", receipt.Warnings);
    }
}
