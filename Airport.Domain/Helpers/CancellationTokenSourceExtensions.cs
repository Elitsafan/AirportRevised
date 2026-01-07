namespace Airport.Domain.Helpers
{
    internal static class CancellationTokenSourceExtensions
    {
        internal static CancellationToken GetToken(this CancellationTokenSource? cts) => cts is null
            ? default
            : cts.Token;
    }
}
