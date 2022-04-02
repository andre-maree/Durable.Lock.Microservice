using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using DurableLockLibrary;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

namespace DurableLockFunctionApp
{
    /// <summary>
    /// This class can be used to quickly create your own type of lock.
    /// Just copy this class and change the LockType value to create a new type of lock.
    /// </summary>
    public static class GenericLockApi
    {
        #region Constants: LockName can be modified to a another name

        const string LockName = "GenericLock";

        #endregion

        #region Api functions

        /// <summary>
        /// Lock with DurableClient
        /// </summary>
        /// <param name="lockId">Lock Id to lock on</param>
        /// <param name="waitForResultSeconds">Specify how long to wait for a result before a 202 is returned, default to 5 seconds if ommited</param>
        /// <returns></returns>
        [FunctionName("Lock")]
        public static async Task<HttpResponseMessage> Lock([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Lock/{LockType}/{LockId}/{waitForResultSeconds:int?}")] HttpRequestMessage req,
                                                                  [DurableClient] IDurableClient client,
                                                                  string lockType,
                                                                  string lockId,
                                                                  int? waitForResultSeconds)
        {
           ////get locks input list from post data
           //List < HttpLockOperation > lockOps = new()
           //{
           //    new HttpLockOperation() { LockId = "a1", LockType = lockType },
           //    new HttpLockOperation() { LockId = "a2", LockType = lockType, PermanentLock = true },
           //    new HttpLockOperation() { LockId = "a3", LockType = lockType }
           //};

            string content = await req.Content.ReadAsStringAsync();

            var mainLockOp = new HttpLockOperation()
            {
                LockId = lockId,
                LockType = lockType
            };

            // no other locks posted, just main lock
            if (string.IsNullOrWhiteSpace(content))
            {
                var result = await ExecuteLock(req, client, waitForResultSeconds, mainLockOp, Constants.Lock);

                return result.HttpLockResponse;
            }

            List<HttpLockOperation> lockOps = JsonSerializer.Deserialize<List<HttpLockOperation>>(content);

            /////////////////////////////// reads //////////////////////////////////
            // 1st just do quick reads on all locks, no need to try do lock orchestrations if a read returns locked 423, exit

            List <Task<HttpResponseMessage>> readTasks = new() { client.ReadDurableLock(LockName, $"{lockType}@{lockId}") };

            foreach (HttpLockOperation loclOp in lockOps)
            {
                readTasks.Add(client.ReadDurableLock(LockName, $"{loclOp.LockType}@{loclOp.LockId}"));
            }

            int readCount = 0;

            while (readCount < readTasks.Count)
            {
                Task<HttpResponseMessage> readTask = await Task.WhenAny<HttpResponseMessage>(readTasks);

                // if any read says locked then bail out
                if (readTask.Result.StatusCode != HttpStatusCode.OK)
                {
                    return new HttpResponseMessage(readTask.Result.StatusCode);
                }

                readCount++;
            }

            /////////////////////// all reads said unlocked, continue below /////////////////////////////////
            // now do lock orhestrations

            List<Task<HttpLockOperation>> lockResponses = new();
            List<Task<HttpLockOperation>> unlockResponses = new();
            List<HttpLockOperation> lockedLi = new();

            foreach (HttpLockOperation lockOp in lockOps)
            {
                lockResponses.Add(ExecuteLock(req, client, waitForResultSeconds, lockOp, Constants.Lock));
            }

            bool hasConflict = false;

            while (lockResponses.Count > 0)
            {
                Task<HttpLockOperation> lockOpTask = await Task.WhenAny<HttpLockOperation>(lockResponses);

                HttpLockOperation lockOp = lockOpTask.Result;

                lockResponses.Remove(lockOpTask);

                if (lockOp.HttpLockResponse.StatusCode == HttpStatusCode.Created)
                {
                    if (hasConflict)
                    {
                        unlockResponses.Add(ExecuteLock(req, client, waitForResultSeconds, lockOp, Constants.UnLock));
                    }
                    else
                    {
                        lockedLi.Add(lockOp);
                    }
                }
                // conflict check
                else if (lockOp.HttpLockResponse.StatusCode == HttpStatusCode.Conflict && !hasConflict)
                {
                    hasConflict = true;

                    // conflict found, wait for all locks and then unlock
                    foreach (HttpLockOperation locked in lockedLi)
                    {
                        unlockResponses.Add(ExecuteLock(req, client, waitForResultSeconds, lockOp, Constants.UnLock));
                    }
                }
            }

            if (hasConflict) // if a conflict was found, wait for the unlocks
            {
                await Task.WhenAll<HttpLockOperation>(unlockResponses);

                return new HttpResponseMessage(HttpStatusCode.Conflict);
            }

            // now it is safe to attempt the main lock
            var lastLock = await ExecuteLock(req, client, waitForResultSeconds, mainLockOp, Constants.Lock);

            // success path
            if (lastLock.HttpLockResponse.StatusCode == HttpStatusCode.Created)
            {
                // unlock the non-permanent locks only
                foreach (HttpLockOperation lockOp in lockedLi.FindAll(l => !l.StayLocked))
                {
                    unlockResponses.Add(ExecuteLock(req, client, waitForResultSeconds, lockOp, Constants.UnLock));
                }

                await Task.WhenAll<HttpLockOperation>(unlockResponses);

                // exit success
                return lastLock.HttpLockResponse;
            }

            // conflict found on the last main lock, unlock all that was locked
            foreach (HttpLockOperation lockOp in lockedLi)
            {
                lockResponses.Add(ExecuteLock(req, client, waitForResultSeconds, lockOp, Constants.UnLock));
            }

            await Task.WhenAll<HttpLockOperation>(lockResponses);

            return new HttpResponseMessage(HttpStatusCode.Conflict);
        }

        private static async Task<HttpLockOperation> ExecuteLock(HttpRequestMessage req, IDurableClient client, int? waitForResultSeconds, HttpLockOperation lockOp, string lockOperstion)
        {
            lockOp.HttpLockResponse = await client.DurableLockOrchestrationStart(req,
                                              "GenericLockOrchestration",
                                                               lockOp.LockType,
                                                               lockOp.LockId,
                                                               waitForResultSeconds,
                                                               lockOperstion);

            return lockOp;
        }

        /// <summary>
        /// Unlock with DurableClient
        /// </summary>
        /// <param name="lockId">Lock Id to lock on</param>
        /// <param name="waitForResultSeconds">Specify how long to wait for a result before a 202 is returned, default to 5 seconds if ommited</param>
        /// <returns></returns>
        [FunctionName("UnLock")]
        public static async Task<HttpResponseMessage> UnLock([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "UnLock/{LockType}/{LockId}/{waitForResultSeconds:int?}")] HttpRequestMessage req,
                                                             [DurableClient] IDurableClient client,
                                                             string lockType,
                                                             string lockId,
                                                             int? waitForResultSeconds)
            => await client.DurableLockOrchestrationStart(req,
                                              "GenericLockOrchestration",
                                              lockType,
                                              lockId,
                                              waitForResultSeconds,
                                              Constants.UnLock);

        /// <summary>
        /// This is used to check if there is a lock with DurableEntityClient
        /// </summary>
        /// <param name="lockId">Lock Id to lock on</param>
        /// <returns></returns>
        [FunctionName("ReadLock")]
        public static async Task<HttpResponseMessage> ReadLock([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ReadLock/{LockType}/{LockId}")] HttpRequestMessage req,
                                                               [DurableClient] IDurableEntityClient client,
                                                               string lockType,
                                                               string lockId)
            => await client.ReadDurableLock(LockName, $"{lockType}@{lockId}");


        /// <summary>
        /// Delete lock state with DurableClient
        /// </summary>
        /// <param name="lockId">Lock Id to lock on</param>
        /// <returns></returns>
        [FunctionName("DeleteLock")]
        public static async Task<HttpResponseMessage> DeleteLock([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "DeleteLock/{LockType}/{LockId}")] HttpRequestMessage req,
                                                                 [DurableClient] IDurableClient client,
                                                                 string lockType,
                                                                 string lockId)
            => await client.DeleteDurableLock(LockName, lockType, lockId);

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
        [FunctionName("GenericLockOrchestration")]
        public static async Task<bool> LockOrchestration([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            return await context.LockOrchestration(LockName);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Re-used method for lock orchestrations
        /// </summary>
        /// <param name="lockId">>Lock Id to lock on</param>
        /// <param name="waitForResultSeconds">Specify how long to wait for a result before a 202 is returned, default is 5 seconds if ommited</param>
        /// <param name="opName">Lock operation name</param>
        /// <returns></returns>
        //private static async Task<HttpResponseMessage> LockOrchestrationStart(HttpRequestMessage req,
        //                                                                        IDurableClient client,
        //                                                                        string lockType,
        //                                                                        string lockId,
        //                                                                        int? waitForResultSeconds,
        //                                                                        string opName)
        //    => await client.DurableLockOrchestrationStart(req,
        //                                             "GenericLockOrchestration",
        //                                             lockType,
        //                                             lockId,
        //                                             waitForResultSeconds,
        //                                             opName);

        #endregion
    }
}