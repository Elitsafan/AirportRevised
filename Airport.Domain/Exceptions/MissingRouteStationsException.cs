namespace Airport.Domain.Exceptions
{
    public class MissingRouteStationsException : InvalidOperationException
    {
        public MissingRouteStationsException()
            : base()
        {
        }

        public MissingRouteStationsException(string? message)
            : base(message)
        {
        }

        public MissingRouteStationsException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
