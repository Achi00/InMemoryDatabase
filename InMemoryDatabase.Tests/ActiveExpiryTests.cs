using InMemoryDatabase.Resp.Enums;
using InMemoryDatabase.Resp.Models;
using InMemoryDatabase.Storage;
using InMemoryDatabase.Tests.Setup;

namespace InMemoryDatabase.Tests
{
    public class ActiveExpiryTests : IAsyncLifetime
    {
        private TestServerFixture _fixture;
        public async Task DisposeAsync()
        {
            await _fixture.DisposeAsync();
        }

        public Task InitializeAsync()
        {
            _fixture = new TestServerFixture();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task ExpireSample_RemovesExpiredKeysFromMemory()
        {
            var store = new RespStore();

            for (int i = 0; i < 100; i++)
            {
                store.Set($"key{i}", RespValue.BulkString("v"),
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
                store.Set($"expiring{i}", RespValue.BulkString("v"),
                    DateTimeOffset.UtcNow.AddMilliseconds(50), SetCondition.Always);
                store.Set($"permanent{i}", RespValue.BulkString("v"),
                    null, SetCondition.Always);
                store.Set($"later{i}", RespValue.BulkString("v"),
                    DateTimeOffset.UtcNow.AddMinutes(5), SetCondition.Always);
            }

            await Task.Delay(150);

            RunSampler(store);

            Assert.Equal(100, store.PhysicalCount);
            Assert.Equal(50, store.VolatileCount);
        }

        [Fact]
        public void SetExpiry_OnPermanentKey_StartsTrackingIt()
        {
            var store = new RespStore();
            // permanent key not tracked by VolatileKeySet
            store.Set("foo", RespValue.BulkString("bar"), null, SetCondition.Always);

            Assert.Equal(0, store.VolatileCount);

            bool result = store.SetExpiry("foo", DateTimeOffset.UtcNow.AddMinutes(1));

            Assert.True(result);
            Assert.Equal(1, store.VolatileCount);
        }

        [Fact]
        public void SetExpiry_OnTrackedKey_StopsTrackingIt()
        {
            var store = new RespStore();
            // permanent key not tracked by VolatileKeySet
            store.Set("foo", RespValue.BulkString("bar"), DateTimeOffset.UtcNow.AddMinutes(1), SetCondition.Always);
            Assert.Equal(1, store.VolatileCount);

            bool result = store.Set("foo", RespValue.BulkString("bar"), null, SetCondition.Always);
            Assert.Equal(0, store.VolatileCount);
            Assert.True(result);
        }
        [Fact]
        public void DelExpiry_OnTrackedKey_StopsTrackingIt()
        {
            var store = new RespStore();
            // permanent key not tracked by VolatileKeySet
            store.Set("foo", RespValue.BulkString("bar"), DateTimeOffset.UtcNow.AddMinutes(1), SetCondition.Always);
            Assert.Equal(1, store.VolatileCount);
            
            bool result = store.Delete("foo");
            Assert.Equal(0, store.VolatileCount);
            Assert.True(result);
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
