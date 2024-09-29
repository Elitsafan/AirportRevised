namespace Airport.Domain.Exceptions
{
    public class LogicNotFoundException : Exception
    {
        public LogicNotFoundException(string? message = null, Exception? inner = null)
            : base(message, inner)
        {
        }
    }
}
