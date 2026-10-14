namespace ElloSaude.Domain.Entities;

public sealed class OutboxMessage
{
    private OutboxMessage() { }

    public OutboxMessage(string queue, string eventType, string tenantId, string payload)
    {
        Id = Guid.NewGuid();
        Queue = queue;
        EventType = eventType;
        TenantId = tenantId;
        Payload = payload;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Queue { get; private set; } = null!;
    public string EventType { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public int Attempts { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public string? LastError { get; private set; }

    public void MarkPublished(DateTime publishedAt)
    {
        PublishedAt = publishedAt;
        NextAttemptAt = null;
        LastError = null;
    }

    public void RegisterFailure(string error)
    {
        Attempts++;
        LastError = error.Length <= 2000 ? error : error[..2000];
        var delaySeconds = Math.Min(300, 5 * Math.Pow(2, Math.Min(Attempts - 1, 6)));
        NextAttemptAt = DateTime.UtcNow.AddSeconds(delaySeconds);
    }
}
