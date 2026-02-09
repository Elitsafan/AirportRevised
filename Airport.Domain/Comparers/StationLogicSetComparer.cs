using System.Diagnostics.CodeAnalysis;

namespace Airport.Domain.Comparers
{
    internal class StationLogicSetComparer : EqualityComparer<ISet<IStationLogic>>
    {
        public override bool Equals(ISet<IStationLogic>? x, ISet<IStationLogic>? y) =>
            x is not null &&
            y is not null &&
            x.SetEquals(y) && y.SetEquals(x);

        public override int GetHashCode([DisallowNull] ISet<IStationLogic> obj)
        {
            int hash = 0;
            if (obj != null)
                foreach (var item in obj)
                    unchecked
                    {
                        hash += item?.GetHashCode() ?? 0;
                    }
            return hash;
        }
    }
}
