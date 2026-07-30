using Airport.Models.DTOs;

namespace Airport.Contracts.Helpers
{
    public interface IGraph<T> : IEnumerable<T>
        where T : IComparable<T>, IEquatable<T>
    {
        void AddEdge(T from, T to);
        bool IsCircular();
        public HashSet<SectionDTO<T>> GetParsedSections(IEnumerable<T> trafficLightIds);
    }
}
