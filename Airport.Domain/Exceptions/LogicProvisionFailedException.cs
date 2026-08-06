namespace Airport.Domain.Exceptions
{
    public class LogicProvisionFailedException : Exception
    {
        public LogicProvisionFailedException()
            : base() { }
        public LogicProvisionFailedException(string message)
            : base(message) { }

        public LogicProvisionFailedException(string message, Exception inner)
            : base(message, inner) { }
    }
}
