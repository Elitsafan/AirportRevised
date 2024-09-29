namespace Airport.Domain.Logics
{
    public class DirectionLogic : IDirectionLogic
    {
        private readonly Direction _direction;

        public DirectionLogic(Direction direction) => _direction = direction;

        public ObjectId From => _direction.From;
        public ObjectId To => _direction.To;

        public override bool Equals(object? obj) => obj is DirectionLogic directionLogic &&
            From == directionLogic.From && 
            To == directionLogic.To;

        public override int GetHashCode() => HashCode.Combine(From, To);
    }
}
