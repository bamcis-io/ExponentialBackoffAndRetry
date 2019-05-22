# BAMCIS Exponential Backoff and Retry Client

A generic implementation of a client that will run a task with exponential backoff between retries. Such a client is extremely useful when making HTTP API calls to a service that throttles requests.

## Table of Contents
- [Usage](#usage)
  * [Client](#client)
  * [Handler](#handler)
- [Revision History](#revision-history)

## Usage

### Client

Import the package:

    using BAMCIS.ExponentialBackoffAndRetry;

A simple example:

    ExponentialBackoffAndRetryClient backoffClient = new ExponentialBackoffAndRetryClient(5, 100);

This creates a client that will attempt at most 5 retries (so 6 attempts total) with the initial delay between retries being 100 ms. As the retries occur the delay goes to 200 ms, 400 ms, and then 800 ms. If the max retries were 7, the following delays would be 1600 ms and then 2000 ms. The reason the last delay is 2000 instead of 3200 like you would expect is because the default max delay is 2000.

If you were to use this:

    ExponentialBackoffAndRetryClient backoffClient = new ExponentialBackoffAndRetryClient(7, 100, 5000);

The max delay is set to 5000 ms, so the last delay would now be 3200 instead of 2000.

By default the exception handling logic treats `TimeoutException`, `OperationCancelledException`, and `HttpRequestException` as retryable exceptions. However, you can provide your own custom exception handling logic in the client to deal with specific use cases. For example:

    ExponentialBackoffAndRetryClient backoffClient = new ExponentialBackoffAndRetryClient(50, 100)
    {
        ExceptionHandlingLogic = (ex) =>
        {
            if (ex is AmazonS3Exception && (AmazonS3Exception(ex)).StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    };

While this is contrived since the AWS .NET SDK provides embedded backoff and retry capabilities, it's  a simple demonstration of how you can control whether or not the retry is invoked.

    GetObjectResponse response = await backoffClient.RunAsync(() => s3Client.GetObjectAsync(bucket, key));

### Handler

You can also provide the custom `HttpMessageHandler` implementation to an `HttpClient` to perform automatic backoff and retry. The `SendAsync` method checks that the status code is successful and if not, throws an `HttpResponseException` that is caught by the default exception handling logic, but can also be caught in your own logic. The `HttpResponseException` exposes the response `StatusCode` property so you can check for specific status codes like 503 in your exception handling.

    HttpClient httpClient = new HttpClient(
        new ExponentialBackoffAndRetryHandler(
            new ExponentialBackoffAndRetryClient()
            {
                Config = new ExponentialBackoffAndRetryConfig()
                {
                    DelayInMilliseconds = 100,
                    MaximumRetries = 5,
                    Jitter = Jitter.NONE
                }
            },
            new HttpMessageHandler()
        )
    );

    HttpResponseMessage response = await httpClient.SendAsync(request);

The `HttpClient` will implement a retry with exponental backoff using no jitter for the requests when any non success status code is returned. You may want to set a custom exception handler for the `BackoffAndRetryConfig` so that you only retry on certain status codes, e.g. you probably don't want to retry for a 404.

## Revision History

### 2.0.0
Added jitter to backoff and changed the client to take a config object. Additionally, added an `HttpMessageHandler` variant so this can be used with an `HttpClient`.

### 1.0.0
Initial release of the library.
