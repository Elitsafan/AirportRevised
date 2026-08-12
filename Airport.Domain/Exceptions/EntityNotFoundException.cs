namespace Airport.Domain.Exceptions
{
    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException()
            : base() { }
        public EntityNotFoundException(string? message)
            : base(message) { }
        public EntityNotFoundException(string? message, Exception? inner = null)
            : base(message, inner) { }
    }
}
