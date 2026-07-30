namespace Airport.Domain.Tests.Logics
{
    public class SyncerLogicTest
    {
        [Fact]
        public void Created_NotNull() => Assert.NotNull(new SyncerLogic(new Syncer(), Mock.Of<ILogger<SyncerLogic>>()));
    }
}
