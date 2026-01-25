namespace Airport.Domain.Exceptions
{
    public class StationEntranceFailedException : Exception
    {
        public StationEntranceFailedException() { }
        public StationEntranceFailedException(string message) : base(message) { }
        public StationEntranceFailedException(string message, Exception inner) : base(message, inner) { }
    }
}
