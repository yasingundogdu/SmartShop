namespace Order.Domain.Outbox;

public enum OutboxStatus
{
    Pending = 0, 
    Processed = 1, 
    Failed = 2
}

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
}