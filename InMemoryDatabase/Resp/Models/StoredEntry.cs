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

        // relies on RespValue's own equality, if not defined should fall back to reflection-based struct comparison by default, reflection based is slower!!
        public bool Equals(StoredEntry other) => ExpiresAt == other.ExpiresAt && Value.Equals(other.Value);

        public override bool Equals(object? obj) => obj is StoredEntry other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ExpiresAt, Value);

    }
}
