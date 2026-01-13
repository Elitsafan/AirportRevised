namespace Airport.Models.Enums
{
    [Flags]
    public enum UpdateResult
    {
        Failed = 0,
        Matched = 1,
        Modified = 2
    }
}