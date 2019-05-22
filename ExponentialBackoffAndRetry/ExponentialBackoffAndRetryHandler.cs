using BAMCIS.ExponentialBackoffAndRetry.Model;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BAMCIS.ExponentialBackoffAndRetry
{
    /// <summary>
    /// An HttpMessageHandler that wraps the SendAsync call in a backoff and retry client
    /// </summary>
    public class ExponentialBackoffAndRetryHandler : DelegatingHandler
    {
        #region Private Fields

        /// <summary>
        /// The backoff and retry client implemented in the handler
        /// </summary>
        public IExponentialBackoffAndRetry client { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor that implements all of the backoff and
        /// retry client defaults
        /// </summary>
        public ExponentialBackoffAndRetryHandler() : base()
        {
            this.client = new ExponentialBackoffAndRetryClient();
        }

        /// <summary>
        /// Creates the handler with the specified cleint
        /// </summary>
        /// <param name="client"></param>
        public ExponentialBackoffAndRetryHandler(IExponentialBackoffAndRetry client) : base()
        {
            this.client = client;
        }

        /// <summary>
        /// Creates the handler that implements all of the backoff and
        /// retry client defaults
        /// </summary>
        /// <param name="innerHandler"></param>
        public ExponentialBackoffAndRetryHandler(HttpMessageHandler innerHandler) : base (innerHandler)
        {
            this.client = new ExponentialBackoffAndRetryClient();
        }

        /// <summary>
        /// Creates the handler with the specified client and message
        /// handler
        /// </summary>
        /// <param name="client"></param>
        /// <param name="innerHandler"></param>
        public ExponentialBackoffAndRetryHandler(IExponentialBackoffAndRetry client, HttpMessageHandler innerHandler) : base(innerHandler)
        {
            this.client = client;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes the SendAsync method wrapped in the backoff and retry client
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await this.client.RunAsync(async () =>  {
                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
                try
                {
                    response.EnsureSuccessStatusCode();
                    return response;
                }
                catch (HttpRequestException e)
                {
                    HttpResponseException temp = new HttpResponseException(e, response.StatusCode);
                    throw temp;
                }             
             });
        }

        #endregion
    }
}
