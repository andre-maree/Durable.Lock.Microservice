using System.Net.Http;
using System.Threading.Tasks;
using DurableLockLibrary;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace DurableLockApi.SharedLockApi
{
    public static class SharedLockApi
    {
        /// <summary>
        /// Get all locks
        /// </summary>
        /// <returns></returns>
        [FunctionName("GetLocks")]
        public static async Task<HttpResponseMessage> GetLocks([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetLocks/{Generic?}/{LockType?}")] HttpRequestMessage req,
                                                               [DurableClient] IDurableClient client,
                                                               string generic,
                                                               string lockType)
            => await client.GetDurableLocks(generic, lockType);
    }
}