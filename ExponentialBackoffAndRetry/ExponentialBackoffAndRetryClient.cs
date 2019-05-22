using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BAMCIS.ExponentialBackoffAndRetry
{
    /// <summary>
    /// Implements generic exponential backoff and retry logic
    /// </summary>
    public class ExponentialBackoffAndRetryClient : IExponentialBackoffAndRetry
    {
        #region Public Properties

        /// <summary>
        /// The client config
        /// </summary>
        public ExponentialBackoffAndRetryConfig Config { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor that uses the default values for the client
        /// config.
        /// </summary>
        public ExponentialBackoffAndRetryClient()
        {
            this.Config = new ExponentialBackoffAndRetryConfig();
        }

        /// <summary>
        /// Creates the client with the specified config.
        /// </summary>
        /// <param name="config"></param>
        public ExponentialBackoffAndRetryClient(ExponentialBackoffAndRetryConfig config)
        {
            this.Config = config ?? throw new ArgumentNullException("config");
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
            ExponentialBackoff backoff = new ExponentialBackoff(this.Config);

            while (true)
            {
                try
                {
                    return await func();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception raised is: {ex.GetType().ToString()} – Message: {ex.Message}");

                    if (this.Config.ExceptionHandlingLogic != null && this.Config.ExceptionHandlingLogic(ex))
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
            ExponentialBackoff backoff = new ExponentialBackoff(this.Config);

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
                    Debug.WriteLine($"Exception raised is: {ex.GetType().ToString()} – Message: {ex.Message}");

                    if (this.Config.ExceptionHandlingLogic(ex))
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
            /// The client config
            /// </summary>
            internal ExponentialBackoffAndRetryConfig Config { get; }

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

            /// <summary>
            /// The random number generator
            /// </summary>
            private Random rand;

            /// <summary>
            /// Used for the decorrelated jitter
            /// </summary>
            private int previousDelay;

            #endregion

            #region Constructors

            /// <summary>
            /// Creates the backoff object
            /// </summary>
            /// <param name="config"></param>
            internal ExponentialBackoff(
                ExponentialBackoffAndRetryConfig config
            )
            {
                this.Config = config;

                // Start at -1 because the first delay is going to add 1 to this,
                // but that attempt does not count as a retry
                this.retries = 0;
                this.exponent = 1;
                this.previousDelay = this.Config.DelayInMilliseconds;
                this.rand = new Random();
            }

            #endregion

            #region Internal Methods

            /// <summary>
            /// Determines the backoff time and waits
            /// </summary>
            /// <returns></returns>
            internal Task Delay()
            {
                if (this.retries == this.Config.MaximumRetries)
                {
                    throw new TimeoutException("Max retry attempts exceeded.");
                }

                // Ensures that the backoff time plateaus so when the system
                // comes back online or we aren't throttled, the next try doesn't
                // take something like 30 min
                int delay = Math.Min(this.Config.DelayInMilliseconds * exponent,
                    this.Config.MaxBackoffInMilliseconds);

                // Apply the specified type of jitter to the delay
                delay = this.ApplyJitter(delay);

                // Increase the number of retries attempted
                ++retries;

                // Make sure we don't create too large a number 
                if (retries < 31)
                {
                    exponent = exponent << 1; // m_pow = Pow(2, m_retries - 1)
                }

                // Perform the actual wait
                return Task.Delay(delay);
            }

            /// <summary>
            /// Resets the current retries and power to their original values
            /// </summary>
            internal void Reset()
            {
                this.retries = 0;
                this.exponent = 1;
                this.previousDelay = this.Config.DelayInMilliseconds;
            }

            /// <summary>
            /// Applies the jitter to the currently calculated delay
            /// </summary>
            /// <param name="delay"></param>
            /// <returns></returns>
            private int ApplyJitter(int delay)
            {
                switch(this.Config.Jitter)
                {
                    default:
                    case Jitter.NONE:
                        {
                            return delay;
                        }
                    case Jitter.SIMPLE:
                        {
                            return delay + rand.Next(0, this.Config.DelayInMilliseconds);
                        }
                    case Jitter.FULL:
                        {
                            return rand.Next(0, delay);
                        }
                    case Jitter.EQUAL:
                        {
                            return (delay / 2) + rand.Next(0, (delay / 2));
                        }
                    case Jitter.DECORRELATED:
                        {
                            int temp = rand.Next(this.Config.DelayInMilliseconds, this.previousDelay * 3);
                            this.previousDelay = temp;
                            return temp;
                        }
                }
            }

            #endregion
        }

        #endregion
    }
}
