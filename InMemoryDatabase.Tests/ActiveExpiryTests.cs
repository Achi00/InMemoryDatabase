using InMemoryDatabase.Resp.Enums;
using InMemoryDatabase.Resp.Models;
using InMemoryDatabase.Storage;

namespace InMemoryDatabase.Tests
{
    public class ActiveExpiryTests
    {
        [Fact]
        public async Task ExpireSample_RemovesExpiredKeysFromMemory()
        {
            var store = new RespStore();

            for (int i = 0; i < 100; i++)
            {
                store.Set($"key{i}", "v",
                    DateTimeOffset.UtcNow.AddMilliseconds(50), SetCondition.Always);
            }

            Assert.Equal(100, store.PhysicalCount);
            // expire time delay
            await Task.Delay(150);

            RunSampler(store);

            Assert.Equal(0, store.PhysicalCount);
            Assert.Equal(0, store.VolatileCount);
        }

        [Fact]
        public async Task ExpireSample_KeepsPermanentAndUnexpiredKeys()
        {
            var store = new RespStore();

            for (int i = 0; i < 50; i++)
            {
                store.Set($"expiring{i}", "v",
                    DateTimeOffset.UtcNow.AddMilliseconds(50), SetCondition.Always);
                store.Set($"permanent{i}", "v",
                    null, SetCondition.Always);
                store.Set($"later{i}", "v",
                    DateTimeOffset.UtcNow.AddMinutes(5), SetCondition.Always);
            }

            await Task.Delay(150);

            RunSampler(store);

            Assert.Equal(100, store.PhysicalCount);
            Assert.Equal(50, store.VolatileCount);
        }

        [Fact]
        public void SetExpiry_OnPermanentKey_StartsTracking()
        {
            var store = new RespStore();
            // permanent key not tracked by VolatileKeySet
            store.Set("foo", "bar", null, SetCondition.Always);

            Assert.Equal(0, store.VolatileCount);

            bool result = store.SetExpiry("foo", DateTimeOffset.UtcNow.AddMinutes(1));

            Assert.True(result);
            Assert.Equal(1, store.VolatileCount);
        }

        [Fact]
        public void Set_WithoutExpiry_OnTrackedKey_StopsTracking()
        {
            var store = new RespStore();
            // permanent key not tracked by VolatileKeySet
            store.Set("foo", "bar", DateTimeOffset.UtcNow.AddMinutes(1), SetCondition.Always);
            Assert.Equal(1, store.VolatileCount);

            bool result = store.Set("foo", "bar", null, SetCondition.Always);
            Assert.Equal(0, store.VolatileCount);
            Assert.True(result);
        }
        [Fact]
        public void Delete_OnTrackedKey_StopsTracking()
        {
            var store = new RespStore();

            store.Set("foo", "bar", DateTimeOffset.UtcNow.AddMinutes(1), SetCondition.Always);
            Assert.Equal(1, store.VolatileCount);
            
            bool result = store.Delete("foo");
            Assert.Equal(0, store.VolatileCount);
            Assert.True(result);
        }

        [Fact]
        public void Set_NxRefused_DoesNotTrackKey()
        {
            var store = new RespStore();
            store.Set("foo", "bar", null, SetCondition.Always);
            Assert.Equal(0, store.VolatileCount);

            // NX with TTL, must be refused because foo exists
            bool result = store.Set("foo", "baz", DateTimeOffset.UtcNow.AddMinutes(1), SetCondition.IfNotExists);
            Assert.False(result);
            Assert.Equal(0, store.VolatileCount);
            Assert.True(store.TryGetString("foo", out var value));
            Assert.Equal("bar", value);
        }
        [Fact]
        public void Set_NxOnMissingKey_WritesAndTracks()
        {
            var store = new RespStore();
            Assert.Equal(0, store.VolatileCount);

            // NX with TTL, must be tracked because foo not exists
            bool result = store.Set("foo", "bar", DateTimeOffset.UtcNow.AddMinutes(1), SetCondition.IfNotExists);
            Assert.True(result);
            Assert.Equal(1, store.VolatileCount);
            Assert.True(store.TryGetString("foo", out var value));
            Assert.Equal("bar", value);
        }

        [Fact]
        public void Set_XxOnMissingKey_WritesAndTracks()
        {
            var store = new RespStore();
            store.Set("foo", "bar", null, SetCondition.Always);
            Assert.Equal(0, store.VolatileCount);

            // XX with TTL, must be tracked because foo exists, checking with different value
            bool result = store.Set("foo", "baz", DateTimeOffset.UtcNow.AddMinutes(1), SetCondition.IfExists);
            Assert.True(result);
            Assert.Equal(1, store.VolatileCount);
            Assert.True(store.TryGetString("foo", out var value));
            Assert.Equal("baz", value);
        }

        [Fact]
        public void Set_XxOnMissingKey_DoesNotTrack()
        {
            var store = new RespStore();
            Assert.Equal(0, store.VolatileCount);

            // XX with TTL, must be refused because foo does not exists
            bool result = store.Set("foo", "bar", DateTimeOffset.UtcNow.AddMinutes(1), SetCondition.IfExists);
            Assert.False(result);
            Assert.Equal(0, store.VolatileCount);
            Assert.Equal(0, store.PhysicalCount);
            Assert.False(store.TryGetString("foo", out var _));
        }

        private static void RunSampler(RespStore store, int rounds = 1000)
        {
            for (int i = 0; i < rounds; i++)
            {
                store.ExpireSample();
            }
        }
    }
}
