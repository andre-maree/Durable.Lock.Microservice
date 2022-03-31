using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using DurableLockLibrary;

namespace DurableLockFunctionApp
{
    /// <summary>
    /// This class can be used to quickly create your own type of lock.
    /// Just copy this class and change the LockType value to create a new type of lock.
    /// </summary>
    public static class DurableLockApi
    {
        // This value is all that needs to change to create your own new lock type
        const string LockType = "MyCustom";

        #region Constants: no need to modify

        const string LockName = LockType + "Lock";
        const string ActionName = "Lock" + LockType;

        #endregion

        #region Api functions

        /// <summary>
        /// Lock with DurableClient
        /// </summary>
        /// <param name="lockId">Lock Id to lock on</param>
        /// <param name="waitForResultSeconds">Specify how long to wait for a result before a 202 is returned, default to 5 seconds if ommited</param>
        /// <returns></returns>
        [FunctionName(ActionName)]
        public static async Task<HttpResponseMessage> Lock([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = ActionName + "/{LockId}/{waitForResultSeconds:int?}")] HttpRequestMessage req,
                                                                  [DurableClient] IDurableClient client,
                                                                  string lockId,
                                                                  int? waitForResultSeconds)
            => await LockOrchestrationStart(req,
                                              client,
                                              lockId,
                                              waitForResultSeconds,
                                              DurableLockHelper.Lock);

        /// <summary>
        /// Unlock with DurableClient
        /// </summary>
        /// <param name="lockId">Lock Id to lock on</param>
        /// <param name="waitForResultSeconds">Specify how long to wait for a result before a 202 is returned, default to 5 seconds if ommited</param>
        /// <returns></returns>
        [FunctionName("Un" + ActionName)]
        public static async Task<HttpResponseMessage> UnLock([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Un" + ActionName + "/{LockId}/{waitForResultSeconds:int?}")] HttpRequestMessage req,
                                                             [DurableClient] IDurableClient client,
                                                             string lockId,
                                                             int? waitForResultSeconds)
            => await LockOrchestrationStart(req,
                                              client,
                                              lockId,
                                              waitForResultSeconds,
                                              DurableLockHelper.UnLock);

        /// <summary>
        /// This is used to check if there is a lock with DurableEntityClient
        /// </summary>
        /// <param name="lockId">Lock Id to lock on</param>
        /// <returns></returns>
        [FunctionName("Read" + LockName)]
        public static async Task<HttpResponseMessage> ReadLock([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Read" + LockName + "/{LockId}")] HttpRequestMessage req,
                                                               [DurableClient] IDurableEntityClient client,
                                                               string lockId)
            => await client.ReadDurableLock(LockType, lockId);


        /// <summary>
        /// Delete lock state with DurableClient
        /// </summary>
        /// <param name="lockId">Lock Id to lock on</param>
        /// <returns></returns>
        [FunctionName("Delete" + LockName)]
        public static async Task<HttpResponseMessage> DeleteLock([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Delete" + LockName + "/{LockId}")] HttpRequestMessage req,
                                                                 [DurableClient] IDurableClient client,
                                                                 string lockId)
            => await client.DeleteDurableLock(LockType, lockId);

        /// <summary>
        /// Get all locks
        /// </summary>
        /// <returns></returns>
        [FunctionName("Get" + LockName + "s")]
        public static async Task<HttpResponseMessage> GetLocks([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Get" + LockName + "s")] HttpRequestMessage req,
                                                               [DurableClient] IDurableClient client)
            => await client.GetDurableLocks(LockType);

        #endregion

        #region Entity functions

        /// <summary>
        /// Generic lock state with DurableEntityContext
        /// </summary>
        [FunctionName(LockName)]
        public static void GenericLock([EntityTrigger] IDurableEntityContext ctx)
            => ctx.CreateLock();

        #endregion

        #region Orchestrations

        /// <summary>
        /// Lock orchestration with DurableOrchestrationContext
        /// </summary>
        /// <returns></returns>
        [Deterministic]
        [FunctionName(LockName + "Orchestration")]
        public static async Task<bool> LockOrchestration([OrchestrationTrigger] IDurableOrchestrationContext context)
            => await context.LockOrchestration(LockName);

        #endregion

        #region Private methods

        /// <summary>
        /// Re-used method for lock orchestrations
        /// </summary>
        /// <param name="lockId">>Lock Id to lock on</param>
        /// <param name="waitForResultSeconds">Specify how long to wait for a result before a 202 is returned, default is 5 seconds if ommited</param>
        /// <param name="opName">Lock operation name</param>
        /// <returns></returns>
        private static async Task<HttpResponseMessage> LockOrchestrationStart(HttpRequestMessage req,
                                                                                IDurableClient client,
                                                                                string lockId,
                                                                                int? waitForResultSeconds,
                                                                                string opName)
            => await client.DurableLockOrchestrationStart(req,
                                                     LockName + "Orchestration",
                                                     LockType,
                                                     lockId,
                                                     waitForResultSeconds,
                                                     opName);

        #endregion
    }
}