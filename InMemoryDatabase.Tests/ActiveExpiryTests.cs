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

        private static void RunSampler(RespStore store, int rounds = 1000)
        {
            for (int i = 0; i < rounds; i++)
            {
                store.ExpireSample();
            }
        }
    }
}
