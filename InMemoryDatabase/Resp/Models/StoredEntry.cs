using InMemoryDatabase.Resp.Models;
using InMemoryDatabase.Storage;

namespace InMemoryDatabase.Resp.Models
{
    // storage which holds respvalue + datetime for TTL adn expiration
    // readonly struct avoids heap allocation other than dictionary's own storage
    public readonly struct StoredEntry : IEquatable<StoredEntry>
    {
        // abstract class which is inherited on types which are design to be storage values, e.g: strings, lists...
        public StoredValue Value { get; }
        public DateTimeOffset? ExpiresAt { get; }

        public StoredEntry(StoredValue value, DateTimeOffset? expiresAt = null)
        {
            Value = value;
            ExpiresAt = expiresAt;
        }

        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;

        // relies on RespValue's own equality, if not defined should fall back to reflection-based struct comparison by default, reflection based is slower!!
        public bool Equals(StoredEntry other) => ReferenceEquals(Value, other.Value) && ExpiresAt == other.ExpiresAt;

        public override bool Equals(object? obj) => obj is StoredEntry other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Value, ExpiresAt);
    }
}
