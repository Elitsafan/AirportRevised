namespace Airport.Domain.Exceptions
{
    public class LogicNotFoundException : Exception
    {
        public LogicNotFoundException()
            : base() { }
        public LogicNotFoundException(string? message)
            : base(message) { }
        public LogicNotFoundException(string? message, Exception? inner)
            : base(message, inner) { }
    }
}
