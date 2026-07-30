namespace Airport.Domain.Helpers
{
    internal static class SyncExtensions
    {
        public static async Task ThrowIfCancellationRequestedAsync(
            this AsyncSemaphore semaphore,
            CancellationTokenSource? cts)
        {
            using var _ = await semaphore.EnterAsync(cts.GetToken());

            cts?.Token.ThrowIfCancellationRequested();

            await (cts?.CancelAsync() ?? Task.CompletedTask);
        }
    }
}
