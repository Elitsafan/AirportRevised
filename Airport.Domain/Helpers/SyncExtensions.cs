namespace Airport.Domain.Helpers
{
    internal static class SyncExtensions
    {
        public static async Task ThrowIfCancellationRequestedAsync(
            this AsyncSemaphore semaphore,
            CancellationTokenSource? cts)
        {
            var releaser = await semaphore.EnterAsync(cts.GetToken());
            try
            {
                cts?.Token.ThrowIfCancellationRequested();
                if (cts != null)
                {
                    await cts.CancelAsync();
                }
            }
            finally { releaser.Dispose(); }
        }
    }
}
