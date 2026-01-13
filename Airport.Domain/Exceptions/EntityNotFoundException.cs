namespace Airport.Domain.Exceptions
{
    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException()
            : base()
        {
        }

        public EntityNotFoundException(string? message, Exception? inner = null)
            : base(message, inner)
        {
        }
    }
}
