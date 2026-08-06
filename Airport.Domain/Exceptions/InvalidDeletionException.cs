namespace Airport.Domain.Exceptions
{
    public class InvalidDeletionException : InvalidOperationException
    {
        public InvalidDeletionException()
            : base() { }
        public InvalidDeletionException(string message)
            : base(message) { }
        public InvalidDeletionException(string message, Exception inner)
            : base(message, inner) { }
    }
}
