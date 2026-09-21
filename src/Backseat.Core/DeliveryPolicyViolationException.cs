namespace Backseat.Core;

public sealed class DeliveryPolicyViolationException : Exception
{
    public DeliveryPolicyViolationException(ActionReceipt receipt, string message)
        : base(message)
    {
        Receipt = receipt ?? throw new ArgumentNullException(nameof(receipt));
    }

    public ActionReceipt Receipt { get; }
}
