namespace BAMCIS.ExponentialBackoffAndRetry
{
    /// <summary>
    /// The types of jitter than can be applied
    /// </summary>
    public enum Jitter
    {
        /// <summary>
        /// Applies no jitter to the backoff
        /// </summary>
        NONE,

        /// <summary>
        /// Chooses a random value between 0 and the base backoff value
        /// </summary>
        SIMPLE,

        /// <summary>
        /// Chooses a random value between 0 and the current exponential backoff value.
        /// This typically causes clients to make less calls, but can take more time to
        /// complete.
        /// </summary>
        FULL,

        /// <summary>
        /// This uses half of the current exponential backoff value as a base and uses
        /// a random value between 0 and half of the exponential backoff as the jitter. 
        /// </summary>
        EQUAL,

        /// <summary>
        /// Similar to full jitter, but increases the maximum jitter based on the last
        /// random value.
        /// </summary>
        DECORRELATED
    }
}
