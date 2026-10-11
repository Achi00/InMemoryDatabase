namespace InMemoryDatabase.Tests
{
    public class VolatileKeySetTests
    {
        private readonly VolatileKeySet _keySet = new VolatileKeySet();

        [Fact]
        public void AddKey_ShouldCountInternaly()
        {
            string key = "test";
            _keySet.Add(key);

            var exists = _keySet.Contains(key);

            Assert.True(exists);
            Assert.Equal(1, _keySet.Count);
        }
    }
}
