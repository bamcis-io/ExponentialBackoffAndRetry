using BAMCIS.ExponentialBackoffAndRetry.Model;
using System;
using System.Net.Http;

namespace BAMCIS.ExponentialBackoffAndRetry
{
    /// <summary>
    /// The config for the ExponentialBackoffAndRetryClient
    /// </summary>
    public class ExponentialBackoffAndRetryConfig
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

        /// <summary>
        /// The type of jitter the backoff and retry client will use
        /// </summary>
        public Jitter Jitter { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor that sets MaximumRetries = 50,
        /// DelayInMilliseconds = 200,
        /// MaxBackoffInMilliseconds = 2000,
        /// Jitter = Jitter.FULL,
        /// and ExceptionHandlingLogic to retry on TimeoutException, HttpRequestException,
        /// and OperationCanceledException
        /// </summary>
        public ExponentialBackoffAndRetryConfig()
        {
            this.MaximumRetries = 50;
            this.DelayInMilliseconds = 200;
            this.MaxBackoffInMilliseconds = 2000;
            this.Jitter = Jitter.FULL;
            this.ExceptionHandlingLogic = (ex) =>
            {
                if (ex is TimeoutException || ex is HttpRequestException || ex is OperationCanceledException || ex is HttpResponseException)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            };
        }

        /// <summary>
        /// Constructor that specifies all available settings
        /// </summary>
        /// <param name="maxRetries"></param>
        /// <param name="delayMilliseconds"></param>
        /// <param name="maxDelayMilliseconds"></param>
        /// <param name="jitter"></param>
        /// <param name="exceptionHandlingLogic"></param>
        public ExponentialBackoffAndRetryConfig(
           int maxRetries,
           int delayMilliseconds,
           int maxDelayMilliseconds,
           Jitter jitter,
           Func<Exception, bool> exceptionHandlingLogic)
        {
            this.MaximumRetries = maxRetries;
            this.DelayInMilliseconds = delayMilliseconds;
            this.MaxBackoffInMilliseconds = maxDelayMilliseconds;
            this.Jitter = jitter;
            this.ExceptionHandlingLogic = exceptionHandlingLogic;
        }

        #endregion
    }
}
