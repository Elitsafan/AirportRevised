namespace Airport.Domain.Creators
{
    internal class DirectionLogicCreator : IDirectionLogicCreator
    {
        private readonly Direction _direction;

        public DirectionLogicCreator(Direction direction) => _direction = direction;

        public IDirectionLogic Create() => new DirectionLogic(_direction);
    }
}
