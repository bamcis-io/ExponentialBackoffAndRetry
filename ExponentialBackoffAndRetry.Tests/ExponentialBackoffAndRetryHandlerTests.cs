using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BAMCIS.ExponentialBackoffAndRetry.Tests
{
    public class ExponentialBackoffAndRetryHandlerTests
    {
        [Fact]
        public async Task TestHttpClientMaxRetriesExceeded()
        {
            // ARRANGE
            HttpRequestMessage failed = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://failed.com")
            };
            HttpRequestMessage success = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://success.com")
            };

            Mock<HttpMessageHandler> client = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            client
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(x => x.RequestUri == new Uri("https://failed.com")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.ServiceUnavailable })
                .Verifiable();

            HttpClient httpClient = new HttpClient(new ExponentialBackoffAndRetryHandler(
                    new ExponentialBackoffAndRetryClient()
                    {
                        Config = new ExponentialBackoffAndRetryConfig()
                        {
                            DelayInMilliseconds = 100,
                            MaximumRetries = 5,
                            Jitter = Jitter.NONE
                        }
                    },
                    client.Object
                )
            );

            // ACT
            // ASSERT
            await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                HttpResponseMessage failedResponse = await httpClient.SendAsync(failed);
            });
        }

        [Fact]
        public async Task TestHttpClientRequestSuccess()
        {
            // ARRANGE
            HttpRequestMessage failed = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://failed.com")
            };
            HttpRequestMessage success = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://success.com")
            };

            HttpResponseMessage failedResponse = new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.ServiceUnavailable
            };
            HttpResponseMessage successResponse = new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.OK
            };

            Mock<HttpMessageHandler> client = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            client
                .Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(x => x.RequestUri == new Uri("https://success.com")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(failedResponse) // first try
                .ReturnsAsync(failedResponse) // first retry
                .ReturnsAsync(failedResponse)
                .ReturnsAsync(failedResponse)
                .ReturnsAsync(failedResponse) // fourth retry
                .ReturnsAsync(successResponse); // fifth retry              

            HttpClient httpClient = new HttpClient(new ExponentialBackoffAndRetryHandler(
                    new ExponentialBackoffAndRetryClient()
                    {
                        Config = new ExponentialBackoffAndRetryConfig()
                        {
                            DelayInMilliseconds = 100,
                            MaximumRetries = 5,
                            Jitter = Jitter.NONE
                        }
                    },
                    client.Object
                )
            );

            // ACT
            Stopwatch sw = new Stopwatch();
            sw.Start();
            HttpResponseMessage res = await httpClient.SendAsync(success);
            sw.Stop();

            // ASSERT
            Assert.True(sw.ElapsedMilliseconds >= 100 + 200 + 400 + 800);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }
    }
}
