# BAMCIS Exponential Backoff and Retry Client

A generic implementation of a client that will run a task with exponential backoff between retries. Such a client is extremely useful when making HTTP API calls to a service that throttles requests.

## Table of Contents
- [Usage](#usage)
- [Revision History](#revision-history)

## Usage

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

While this is contrived since the AWS .NET SDK provided embedded backoff and retry capabilities, it's  a simple demonstration of how you can control whether or not the retry is invoked.

## Revision History

### 1.0.0
Initial release of the library.
