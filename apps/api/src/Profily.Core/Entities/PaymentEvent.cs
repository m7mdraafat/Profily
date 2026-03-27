namespace Profily.Core.Entities;

public sealed class PaymentEvent
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? PaymobOrderId { get; set; }
    public string? TransactionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int? AmountCents { get; set; }
    public string? Currency { get; set; }
    public string? RawPayload { get; set; }
    public DateTime CreatedAt { get; set; }
}
