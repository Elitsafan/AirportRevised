using System.Diagnostics.CodeAnalysis;

namespace Airport.Domain.Comparers
{
    internal class StationLogicSetComparer : EqualityComparer<ISet<IStationLogic>>
    {
        public override bool Equals(ISet<IStationLogic>? x, ISet<IStationLogic>? y) =>
            x is not null &&
            y is not null &&
            x.SetEquals(y) && y.SetEquals(x);

        public override int GetHashCode([DisallowNull] ISet<IStationLogic> obj) =>
            obj.Aggregate(typeof(IStationLogic).GetHashCode(), (hash, sl) => HashCode.Combine(hash, sl));
    }
}
