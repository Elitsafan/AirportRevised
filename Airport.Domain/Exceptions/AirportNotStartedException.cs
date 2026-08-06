namespace Airport.Domain.Exceptions
{
    public class AirportNotStartedException : Exception
    {
        public AirportNotStartedException()
            : base() { }
        public AirportNotStartedException(string message)
            : base(message) { }

        public AirportNotStartedException(string message, Exception inner)
            : base(message, inner) { }
    }
}
