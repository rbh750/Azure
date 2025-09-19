namespace Service.Azure.RetryPolicy
{
    /// <summary>
    /// Custom retry policy service that implements exponential backoff.
    /// Note: Retry policies are usually implemented through Polly library, but this is a custom implementation.
    /// </summary>
    public interface IRetryPolicyService
    {
        /// <summary>
        /// Sets up the retry settings with max retries and delay times.
        /// </summary>
        void Configure(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds);

        /// <summary>
        /// Runs an action and retries it if it fails, with increasing delays between attempts.
        /// </summary>
        void Run(Action action);

        /// <summary>
        /// Runs an async task and retries it if it fails, with increasing delays between attempts.
        /// </summary>
        Task RunAsync(Func<Task> func);

        /// <summary>
        /// Runs an async function that returns a value and retries it if it fails, with increasing delays between attempts.
        /// </summary>
        Task<T> RunAsync<T>(Func<Task<T>> func);
    }
}