namespace InMemoryDatabase.Tests
{
    public class VolatileKeySetTests
    {
        private readonly VolatileKeySet _keySet = new VolatileKeySet();

        [Fact]
        public void AddKey_ShouldIncrementIncrementCount()
        {
            string key = "test";
            _keySet.Add(key);

            var exists = _keySet.Contains(key);

            Assert.True(exists);
            Assert.Equal(1, _keySet.Count);
        }

        [Fact]
        public void AddExistingKey_CountShouldStaySame()
        {
            string key = "test";
            _keySet.Add(key);

            var exists = _keySet.Contains(key);

            Assert.True(exists);
            Assert.Equal(1, _keySet.Count);

            _keySet.Add(key);
            Assert.Equal(1, _keySet.Count);
        }

        [Fact]
        public void RemoveNonExistingKey_ShouldChangeNothing()
        {
            string key = "test";
            _keySet.Remove(key);

            var exists = _keySet.Contains(key);
            Assert.False(exists);
            Assert.Equal(0, _keySet.Count);
        }

        [Fact]
        public void RemoveOnlyKey_ShouldDecrementCount()
        {
            string key = "test";
            _keySet.Add(key);

            bool exists;

            exists = _keySet.Contains(key);

            Assert.True(exists);
            Assert.Equal(1, _keySet.Count);

            _keySet.Remove(key);

            exists = _keySet.Contains(key);
            // key already removed should not found it
            Assert.False(exists);
            Assert.Equal(0, _keySet.Count);
        }

        [Fact]
        public void RemoveLastKey_ShouldSkippSwap()
        {
            string key1 = "test";
            string key2 = "test2";
            _keySet.Add(key1);
            _keySet.Add(key2);

            Assert.Equal(2, _keySet.Count);

            _keySet.Remove(key2);
            Assert.Equal(1, _keySet.Count);

        }
    }
}
