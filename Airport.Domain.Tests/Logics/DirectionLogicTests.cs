namespace Airport.Domain.Tests.Logics
{
    public class DirectionLogicTests
    {
        [Fact]
        public void Created_NotNull() => Assert.NotNull(new DirectionLogic(new Direction()));
    }
}
