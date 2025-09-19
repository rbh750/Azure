using Microsoft.Extensions.Options;

namespace Service.Azure.RetryPolicy;

public class RetryPolicyService(IOptions<RetryPolicyServiceSettings> retryPolicyServiceSettings) : IRetryPolicyService
{
    ExponentialBackoff exponentialBackoff = new(
        retryPolicyServiceSettings.Value.MaxRetries,
        retryPolicyServiceSettings.Value.DelayMilliseconds,
        retryPolicyServiceSettings.Value.MaxDelayMilliseconds);


    /// <inheritdoc />
    public void Configure(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds)
    {
        exponentialBackoff = new ExponentialBackoff(maxRetries, delayMilliseconds, maxDelayMilliseconds);
    }

    /// <inheritdoc />
    public void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            exponentialBackoff.Delay(ex);
            Run(action);
        }
    }

    /// <inheritdoc />
    public async Task RunAsync(Func<Task> func)
    {
        try
        {
            await func();
        }
        catch (Exception ex)
        {
            await exponentialBackoff.Delay(ex);
            await RunAsync(func);
        }
    }

    /// <inheritdoc />
    public async Task<T> RunAsync<T>(Func<Task<T>> func)
    {
        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            await exponentialBackoff.Delay(ex);
            return await RunAsync(func);
        }
    }

    // Handles the exponential backoff delay calculation and retry counting for failed operations.
    private struct ExponentialBackoff(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds)
    {
        private readonly int maxRetries = maxRetries;
        private readonly int delayMilliseconds = delayMilliseconds;
        private readonly int maxDelayMilliseconds = maxDelayMilliseconds;
        private int retries = 0;
        private int pow = 1;

        public Task Delay(Exception lastError)
        {
            if (retries == maxRetries)
            {
                throw lastError;
            }

            ++retries;

            if (retries < 31)
            {
                pow <<= 1; // m_pow = Pow(2, m_retries - 1)
            }
            int delay = Math.Min(delayMilliseconds * (pow - 1) / 2, maxDelayMilliseconds);
            return Task.Delay(delay);
        }
    }
}