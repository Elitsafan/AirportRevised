namespace Airport.Domain.Factories
{
    public class DirectionLogicFactory : IDirectionLogicFactory
    {
        public IDirectionLogicCreator GetCreator(Direction direction) => direction is null
            ? throw new ArgumentNullException(nameof(direction))
            : new DirectionLogicCreator(direction);
    }
}
