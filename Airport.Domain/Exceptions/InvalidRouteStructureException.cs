namespace Airport.Domain.Exceptions
{
    public class InvalidRouteStructureException : InvalidOperationException
    {
        public InvalidRouteStructureException()
            : base()
        {
        }

        public InvalidRouteStructureException(string? message)
            : base(message)
        {
        }

        public InvalidRouteStructureException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
