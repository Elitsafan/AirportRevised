using System.Diagnostics.CodeAnalysis;

namespace Airport.Domain.Comparers
{
    internal class RouteSectionComparer : EqualityComparer<IRouteSection>
    {
        public override bool Equals(IRouteSection? x, IRouteSection? y) =>
            x is not null &&
            y is not null &&
            x.RouteId == y.RouteId &&
            x.Source.SetEquals(x.Source) &&
            x.Destination.SetEquals(x.Destination);

        public override int GetHashCode([DisallowNull] IRouteSection obj) => HashCode.Combine(
            obj.RouteId.GetHashCode(),
            obj.Source.Aggregate(typeof(IStationLogic).GetHashCode(), (hash, sl) => HashCode.Combine(hash, sl)),
            obj.Destination.Aggregate(typeof(IStationLogic).GetHashCode(), (hash, sl) => HashCode.Combine(hash, sl)));
    }
}
