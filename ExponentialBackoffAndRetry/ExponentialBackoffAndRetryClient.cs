using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace BAMCIS.ExponentialBackoffAndRetry
{
    /// <summary>
    /// Implements generic exponential backoff and retry logic
    /// </summary>
    public class ExponentialBackoffAndRetryClient
    {
        #region Public Properties

        /// <summary>
        /// The maximum number of times the function will be retried
        /// </summary>
        public int MaximumRetries { get; set; }

        /// <summary>
        /// The base delay in milliseconds
        /// </summary>
        public int DelayInMilliseconds { get; set; }

        /// <summary>
        /// The maximum delay in milliseconds. This is the plateau value.
        /// </summary>
        public int MaxBackoffInMilliseconds { get; set; }

        /// <summary>
        /// The logic that takes a raised exception and determines whether or
        /// not the request should be retried using exponential backoff. If the
        /// function returns false, the request is not retried and the exception
        /// is thrown.
        /// </summary>
        public Func<Exception, bool> ExceptionHandlingLogic { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor that has default values. Sets
        /// MaximumRetries to 50, DelayInMilliseconds to 200, 
        /// and MaximumBackoffInMilliseconds to 2000. By default the exception
        /// handling logic returns true if the exception is a TimeoutException,
        /// HttpRequestException, or an OperationCanceledException.
        /// </summary>
        /// <param name="maxRetries"></param>
        /// <param name="delayMilliseconds"></param>
        /// <param name="maxDelayMilliseconds"></param>
        public ExponentialBackoffAndRetryClient(
            int maxRetries = 50,
            int delayMilliseconds = 200,
            int maxDelayMilliseconds = 2000)
        {
            this.MaximumRetries = maxRetries;
            this.DelayInMilliseconds = delayMilliseconds;
            this.MaxBackoffInMilliseconds = maxDelayMilliseconds;
            this.ExceptionHandlingLogic = (ex) =>
            {
                if (ex is TimeoutException || ex is HttpRequestException || ex is OperationCanceledException)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Runs the function async with exponential backoff
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public async Task<T> RunAsync<T>(Func<Task<T>> func)
        {
            ExponentialBackoff backoff = new ExponentialBackoff(
                this.MaximumRetries,
                this.DelayInMilliseconds,
                this.MaxBackoffInMilliseconds);

            while (true)
            {
                try
                {
                    return await func();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception raised is: {ex.GetType().ToString()} – Message: {ex.Message}");

                    if (this.ExceptionHandlingLogic != null && this.ExceptionHandlingLogic(ex))
                    {
                        await backoff.Delay();
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
        }

        /// <summary>
        /// Runs the function async with exponential backoff
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public async Task RunAsync(Func<Task> func)
        {
            ExponentialBackoff backoff = new ExponentialBackoff(
                this.MaximumRetries,
                this.DelayInMilliseconds,
                this.MaxBackoffInMilliseconds
            );

            bool shouldContinue = true;

            while (shouldContinue)
            {
                try
                {
                    await func();
                    shouldContinue = false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception raised is: {ex.GetType().ToString()} –Message: {ex.Message}");

                    if (this.ExceptionHandlingLogic(ex))
                    {
                        await backoff.Delay();
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
        }

        #endregion

        #region Private Struct

        /// <summary>
        /// A new exponential backoff struct is created each time
        /// a task is run async
        /// </summary>
        private struct ExponentialBackoff
        {
            #region Public Properties

            /// <summary>
            /// The maximum number of retries, the original execution is not
            /// counted as a retry
            /// </summary>
            internal int MaxRetries { get; }

            /// <summary>
            /// The base delay in milliseconds
            /// </summary>
            internal int DelayInMilliseconds { get; }

            /// <summary>
            /// The maximum allowable delay in milliseconds
            /// </summary>
            internal int MaximumBackoffInMilliseconds { get; }

            #endregion

            #region Private Fields

            /// <summary>
            /// The current number of retries
            /// </summary>
            private int retries;

            /// <summary>
            /// The current power for backoff
            /// </summary>
            private int exponent;

            #endregion

            #region Constructors

            internal ExponentialBackoff(
                int maximumRetries,
                int delayInMilliseconds,
                int maximumBackoffInMilliseconds
            )
            {
                this.MaxRetries = maximumRetries;
                this.DelayInMilliseconds = delayInMilliseconds;
                this.MaximumBackoffInMilliseconds = maximumBackoffInMilliseconds;

                // Start at -1 because the first delay is going to add 1 to this,
                // but that attempt does not count as a retry
                this.retries = 0;
                this.exponent = 1;
            }

            #endregion

            #region Internal Methods

            internal Task Delay()
            {
                if (this.retries == this.MaxRetries)
                {
                    throw new TimeoutException("Max retry attempts exceeded.");
                }

                // Ensures that the backoff time plateaus so when the system
                // comes back online or we aren't throttled, the next try doesn't
                // take something like 30 min
                int delay = Math.Min(this.DelayInMilliseconds * exponent,
                    this.MaximumBackoffInMilliseconds);

                ++retries;

                if (retries < 31)
                {
                    exponent = exponent << 1; // m_pow = Pow(2, m_retries - 1)
                }

                return Task.Delay(delay);
            }

            /// <summary>
            /// Resets the current retries and power to their original values
            /// </summary>
            internal void Reset()
            {
                this.retries = 0;
                this.exponent = 1;
            }

            #endregion
        }

        #endregion
    }
}
