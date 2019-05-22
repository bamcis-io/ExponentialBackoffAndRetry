using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BAMCIS.ExponentialBackoffAndRetry.Tests
{
    public class ExponentialBackoffAndRetryClientTests
    {
        [Fact]
        public async Task ClientTest()
        {
            // ARRANGE
            ExponentialBackoffAndRetryClient backoffClient = new ExponentialBackoffAndRetryClient(
                new ExponentialBackoffAndRetryConfig()
                {
                    MaximumRetries = 5,
                    DelayInMilliseconds = 100,
                    Jitter = Jitter.NONE
                }
            );
            Stopwatch sw = new Stopwatch();

            // ACT
            sw.Start();
            int i = 0;

            await backoffClient.RunAsync(() => {

                Interlocked.Increment(ref i);

                if (i < 5)
                { 
                    throw new TimeoutException();
                }

                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            });

            // ASSERT
            Assert.True(sw.ElapsedMilliseconds >= 100 + 200 + 400 + 800);
        }

        [Fact]
        public async Task ManyRetriesWithLongMaxDelay()
        {
            // ARRANGE
            ExponentialBackoffAndRetryClient backoffClient = new ExponentialBackoffAndRetryClient(
                new ExponentialBackoffAndRetryConfig()
                {
                    MaximumRetries = 50,
                    DelayInMilliseconds = 100,
                    MaxBackoffInMilliseconds = 10000,
                    Jitter = Jitter.NONE
                }
            );
            Stopwatch sw = new Stopwatch();
            Action test = () => { };

            // ACT
            sw.Start();
            int i = 0;

            await backoffClient.RunAsync(() => {

                Interlocked.Increment(ref i);

                if (i < 8)
                {
                    throw new TimeoutException();
                }

                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            });

            // ASSERT
            Assert.True(sw.ElapsedMilliseconds >= 100 + 200 + 400 + 800 + 1600 + 3200 + 6400);
        }

        [Fact]
        public async Task ManyRetriesWithStandardMaxDelay()
        {
            // ARRANGE
            ExponentialBackoffAndRetryClient backoffClient = new ExponentialBackoffAndRetryClient(
                new ExponentialBackoffAndRetryConfig()
                {
                    MaximumRetries = 50,
                    DelayInMilliseconds = 100,
                    Jitter = Jitter.NONE
                }
            );
            Stopwatch sw = new Stopwatch();
            Action test = () => { };

            // ACT
            sw.Start();
            int i = 0;

            await backoffClient.RunAsync(() => {

                Interlocked.Increment(ref i);

                if (i < 8)
                {
                    throw new TimeoutException();
                }

                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            });

            // ASSERT
            Assert.True(sw.ElapsedMilliseconds >= 100 + 200 + 400 + 800 + 1600 + 2000 + 2000);
        }

        [Fact]
        public async Task CustomExceptionHandling()
        {
            // ARRANGE
            ExponentialBackoffAndRetryClient backoffClient = new ExponentialBackoffAndRetryClient(
                new ExponentialBackoffAndRetryConfig()
                {
                    MaximumRetries = 50,
                    DelayInMilliseconds = 100,
                    Jitter = Jitter.NONE,
                    ExceptionHandlingLogic = (ex) =>
                    {
                        if (ex is InvalidOperationException)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            );

            Stopwatch sw = new Stopwatch();
            Action test = () => { };

            // ACT
            sw.Start();
            int i = 0;

            await backoffClient.RunAsync(() => {

                Interlocked.Increment(ref i);

                if (i < 5)
                {
                    throw new InvalidOperationException();
                }

                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            });

            // ASSERT
            Assert.True(sw.ElapsedMilliseconds >= 100 + 200 + 400 + 800);
        }

        [Fact]
        public async Task CustomExceptionHandlingFail()
        {
            // ARRANGE
            ExponentialBackoffAndRetryClient backoffClient = new ExponentialBackoffAndRetryClient(
                new ExponentialBackoffAndRetryConfig()
                {
                    MaximumRetries = 50,
                    DelayInMilliseconds = 100,
                    Jitter = Jitter.NONE,
                    ExceptionHandlingLogic = (ex) =>
                    {
                        if (ex is InvalidOperationException)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            );

            Stopwatch sw = new Stopwatch();
            Action test = () => { };

            // ACT
            sw.Start();
            int i = 0;

            // ASSERT
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await backoffClient.RunAsync(() => {

                    Interlocked.Increment(ref i);

                    if (i < 5)
                    {
                        throw new ArgumentException();
                    }

                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
                })
            );
        }

        [Fact]
        public async Task RetriesExceeded()
        {
            // ARRANGE
            ExponentialBackoffAndRetryClient backoffClient = new ExponentialBackoffAndRetryClient(
               new ExponentialBackoffAndRetryConfig()
               {
                   MaximumRetries = 5,
                   DelayInMilliseconds = 100,
                   Jitter = Jitter.NONE
               }
            );
            Stopwatch sw = new Stopwatch();

            // ACT
            sw.Start();

            // ASSERT
            await Assert.ThrowsAsync<TimeoutException>(async () =>
            await backoffClient.RunAsync(() => {
                    throw new TimeoutException();
            }));

            // ASSERT
            Assert.True(sw.ElapsedMilliseconds >= 100 + 200 + 400 + 800);
        }
    }
}
