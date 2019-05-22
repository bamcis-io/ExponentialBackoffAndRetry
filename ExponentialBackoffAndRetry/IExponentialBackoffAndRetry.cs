using System;
using System.Threading.Tasks;

namespace BAMCIS.ExponentialBackoffAndRetry
{
    public interface IExponentialBackoffAndRetry
    {
        Task<T> RunAsync<T>(Func<Task<T>> func);

        Task RunAsync(Func<Task> func);
    }
}
