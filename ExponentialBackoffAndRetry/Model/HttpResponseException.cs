using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace BAMCIS.ExponentialBackoffAndRetry.Model
{
    public class HttpResponseException : HttpRequestException
    {
        #region Public Properties

        public HttpStatusCode StatusCode { get; set; }

        #endregion

        #region Constructors

        public HttpResponseException(HttpRequestException originalException, HttpStatusCode statusCode) : base(originalException.Message, originalException.InnerException)
        {
            this.StatusCode = statusCode;
        }

        #endregion
    }
}
