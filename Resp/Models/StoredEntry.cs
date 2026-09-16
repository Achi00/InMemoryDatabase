using InMemoryDatabase.Parser.Models;

namespace InMemoryDatabase.Resp.Models
{
    // storage which holds respvalue + datetime for TTL adn expiration
    // readonly struct avoids heap allocation other than dictionary's own storage
    public readonly struct StoredEntry
    {
        public RespValue Value { get; }
        public DateTimeOffset? ExpiresAt { get; }

        public StoredEntry(RespValue value, DateTimeOffset? expiresAt = null)
        {
            Value = value;
            ExpiresAt = expiresAt;
        }

        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;
    }
}
