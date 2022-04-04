using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using DurableLockLibrary;
using System.Collections.Generic;
using System;
using System.Net;
using System.Text.Json;
using DurableTask.Core;

namespace DurableLockFunctionApp
{
    /// <summary>
    /// This class can be used to quickly create your own type of lock.
    /// Just copy this class and change the LockType value to create a new type of lock.
    /// </summary>
    public static class GenericLockApi2
    {
        #region Constants: LockName can be modified to a another name

        const string LockName = "GenericLock";

        #endregion

        #region Api functions

        [FunctionName("Clean")]
        public static async Task<HttpResponseMessage> Clean([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Clean")] HttpRequestMessage req,
                                                                  [DurableClient] IDurableEntityClient client)
        {
            var r = await client.CleanEntityStorageAsync(true, true, new System.Threading.CancellationToken());

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
        /// <summary>
        /// Lock with DurableClient
        /// </summary>
        /// <param name="lockId">Lock Id to lock on</param>
        /// <param name="waitForResultSeconds">Specify how long to wait for a result before a 202 is returned, default to 5 seconds if ommited</param>
        /// <returns></returns>
        [FunctionName("Lock2")]
        public static async Task<HttpResponseMessage> Lock([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Lock2/{waitForResultSeconds:int?}")] HttpRequestMessage req,
                                                                  [DurableClient] IDurableOrchestrationClient client,
                                                                  int? waitForResultSeconds)
        {
            try
            {
                var sr = await req.Content.ReadAsStringAsync();
                List<LockOperation> lockOps = JsonSerializer.Deserialize<List<LockOperation>>(sr);
                var guid = Guid.NewGuid().ToString();
                var rr = await client.StartNewAsync("MainLockOrchestration", guid, lockOps);

                return await client.WaitForCompletionOrCreateCheckStatusResponseAsync(req,
                                                                                   guid,
                                                                                   TimeSpan.FromSeconds(waitForResultSeconds is null
                                                                                   ? 5
                                                                                   : waitForResultSeconds.Value));



            }
            catch (Exception ex)
            {
                var r = 0;
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }


        /// <summary>
        /// Lock orchestration with DurableOrchestrationContext
        /// </summary>
        /// <returns></returns>
        [Deterministic]
        [FunctionName("MainLockOrchestration")]
        public static async Task<LockResult> MainLockOrchsestration([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            List<string> lockList = new();
            try
            {
                var lockOps = context.GetInput<List<LockOperation>>();

                List<Task<LockOperationResult>> lockResponses = new();
                List<Task> unlockResponses = new();
                List<LockOperation> lockedLi = new();
                List<LockOperation> successLi = new();

                foreach (LockOperation lockOp in lockOps)
                {
                    // call sub orch
                    lockResponses.Add(context.CallSubOrchestratorAsync<LockOperationResult>("GenericLockOrchestration2", $"{lockOp.LockType}@{lockOp.LockId}", "lock"));
                    //lockResponses.Add(client.ExecuteLock(req, orchestratioName, waitForResultSeconds, lockOp, Constants.Lock, genericMode));
                }

                await Task.WhenAll(lockResponses);

                //List<Task<LockOperation>> successLocks = lockResponses.FindAll(r => r.Result.StayLocked == true);

                //if (successLocks.Count != lockOps.Count)
                //{
                var lockedItems = new List<string>();
                List<Task<LockOperationResult>> list = lockResponses;

                foreach (Task<LockOperationResult> lockOp in list)
                {
                    if (lockOp.Result.IsLocked)
                    {
                        successLi.Add(lockOp.Result);
                    }
                    else
                    {
                        lockOp.Result.IsLocked = true;
                        lockedLi.Add(lockOp.Result);
                    }
                }

                //await Task.WhenAll(unlockResponses);

                // conflicts
                if (lockedLi.Count > 0)
                {
                    foreach (var lockOp in successLi)
                    {
                        unlockResponses.Add(context.CallSubOrchestratorAsync<LockOperation>("GenericLockOrchestration2", $"{lockOp.LockType}@{lockOp.LockId}", "unlock"));
                    }

                    await Task.WhenAll(unlockResponses);

                    return new LockResult()
                    {
                        HttpStatusCode = 429,
                        Locks = lockedLi
                    };
                }


                //foreach (var lockOp in lockOps.FindAll(l => !l.StayLocked))
                //{
                //    unlockResponses.Add(context.CallSubOrchestratorAsync<LockOperation>("GenericLockOrchestration2", $"{lockOp.LockType}@{lockOp.LockId}", "unlock"));
                //    successLi.Remove(LockName + "/" + lockOp.LockType + "/" + lockOp.LockId);
                //}

                //await Task.WhenAll(unlockResponses);

                //foreach (var lockOp in successLocks)
                //{
                //    lockList.Add(LockName + "/" + lockOp.Result.LockType + "/" + lockOp.Result.LockId);
                //}

                return new LockResult() { HttpStatusCode = 201, Locks = successLi };
            }
            catch (Exception ex)
            {
                var r = 0;
                return new LockResult() { HttpStatusCode = 500 };
            }
        }

        [FunctionName("UnLock2")]
        public static async Task<HttpResponseMessage> UnLock2([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "UnLock2/{waitForResultSeconds:int?}")] HttpRequestMessage req,
                                                                  [DurableClient] IDurableOrchestrationClient client,
                                                                  int? waitForResultSeconds)
        {
            try
            {
                var sr = await req.Content.ReadAsStringAsync();
                List<LockOperation> lockOps = JsonSerializer.Deserialize<List<LockOperation>>(sr);
                var guid = Guid.NewGuid().ToString();
                var rr = await client.StartNewAsync("UnLock2Orchestration", guid, lockOps);

                return await client.WaitForCompletionOrCreateCheckStatusResponseAsync(req,
                                                                                   guid,
                                                                                   TimeSpan.FromSeconds(waitForResultSeconds is null
                                                                                   ? 5
                                                                                   : waitForResultSeconds.Value));



            }
            catch (Exception ex)
            {
                var r = 0;
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
        /// <summary>
        /// Lock orchestration with DurableOrchestrationContext
        /// </summary>
        /// <returns></returns>
        [Deterministic]
        [FunctionName("UnLock2Orchestration")]
        public static async Task<LockResult> UnLockOrchestration([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            try
            {
                var lockOps = context.GetInput<List<LockOperation>>();

                List<Task> unlockResponses = new();
                

                foreach (LockOperation lockOp in lockOps)
                {
                    // call sub orch
                    unlockResponses.Add(context.CallSubOrchestratorAsync<LockOperationResult>("GenericLockOrchestration2", $"{lockOp.LockType}@{lockOp.LockId}", "unlock"));
                }

                await Task.WhenAll(unlockResponses);

                return new LockResult() { HttpStatusCode = 200 };
            }
            catch (Exception ex)
            {
                var r = 0;
                return new LockResult() { HttpStatusCode = 500 };
            }
        }

        #region Orchestrations

        /// <summary>
        /// Lock orchestration with DurableOrchestrationContext
        /// </summary>
        /// <returns></returns>
        [Deterministic]
        [FunctionName("GenericLockOrchestration2")]
        public static async Task<LockOperation> LockOrchestration([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            return await context.LockOrchestration(LockName);
        }

        #endregion

        /// <summary>
        /// Unlock with DurableClient
        /// </summary>
        /// <param name="lockId">Lock Id to lock on</param>
        /// <param name="waitForResultSeconds">Specify how long to wait for a result before a 202 is returned, default to 5 seconds if ommited</param>
        /// <returns></returns>
        [FunctionName("UnLock")]
        public static async Task<HttpResponseMessage> UnLock([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "UnLock/{LockType}/{LockId}/{waitForResultSeconds:int?}")] HttpRequestMessage req,
                                                             [DurableClient] IDurableOrchestrationClient client,
                                                             string lockType,
                                                             string lockId,
                                                             int? waitForResultSeconds)
            => await client.DurableLockOrchestrationStart(req,
                                              "GenericLockOrchestration",
                                              lockType,
                                              lockId,
                                              waitForResultSeconds,
                                              Constants.UnLock,
                                              true);

        ///// <summary>
        ///// This is used to check if there is a lock with DurableEntityClient
        ///// </summary>
        ///// <param name="lockId">Lock Id to lock on</param>
        ///// <returns></returns>
        //[FunctionName("ReadLock")]
        //public static async Task<HttpResponseMessage> ReadLock([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ReadLock/{LockType}/{LockId}")] HttpRequestMessage req,
        //                                                       [DurableClient] IDurableEntityClient client,
        //                                                       string lockType,
        //                                                       string lockId)
        //    => await client.ReadDurableLock(LockName, $"{lockType}@{lockId}");



        ///// <summary>
        ///// Delete lock state with DurableClient
        ///// </summary>
        ///// <param name="lockId">Lock Id to lock on</param>
        ///// <returns></returns>
        //[FunctionName("DeleteLock")]
        //public static async Task<HttpResponseMessage> DeleteLock([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "DeleteLock/{LockType}/{LockId}")] HttpRequestMessage req,
        //                                                         [DurableClient] IDurableClient client,
        //                                                         string lockType,
        //                                                         string lockId)
        //    => await client.DeleteDurableLock(LockName, lockType, lockId);

        //#endregion

        //#region Entity functions

        ///// <summary>
        ///// Generic lock state with DurableEntityContext
        ///// </summary>
        [FunctionName(LockName)]
        public static void GenericLock([EntityTrigger] IDurableEntityContext ctx)
            => ctx.CreateLock();

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