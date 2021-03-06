using Durable.Lock.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Durable.Lock.Api
{
    /// <summary>
    /// This class is generic to handle any locks defined
    /// </summary>
    public static class DurableLockApi
    {
        #region Api functions: Durable orchestration and entity clients

        /// <summary>
        /// Lock with DurableClient
        /// </summary>
        /// <param name="lockId">Lock Id to lock on</param>
        /// <param name="waitForResultSeconds">Specify how long to wait for a result before a 202 is returned, default to 5 seconds if ommited</param>
        /// <returns></returns>
        [FunctionName("Lock")]
        public static async Task<HttpResponseMessage> Lock([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "Lock/{waitForResultSeconds:int?}")] HttpRequestMessage req,
                                                                  [DurableClient] IDurableOrchestrationClient client,
                                                                  int? waitForResultSeconds)
        {
            try
            {
                string input = await req.Content.ReadAsStringAsync();
                
                string guid = Guid.NewGuid().ToString();

                await client.StartNewAsync("MainLockOrchestration", guid, input);

                return await client.WaitForCompletionOrCreateCheckStatusResponseAsync(req,
                                                                                   guid,
                                                                                   TimeSpan.FromSeconds(waitForResultSeconds is null
                                                                                   ? 5
                                                                                   : waitForResultSeconds.Value));
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }



        /// <summary>
        /// Unlock a list of locks posted in
        /// </summary>
        /// <param name="req"></param>
        /// <param name="client"></param>
        /// <param name="waitForResultSeconds"></param>
        /// <returns></returns>
        [FunctionName("UnLock")]
        public static async Task<HttpResponseMessage> UnLock([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "UnLock/{waitForResultSeconds:int?}")] HttpRequestMessage req,
                                                                  [DurableClient] IDurableOrchestrationClient client,
                                                                  int? waitForResultSeconds)
        {
            try
            {

                string input = await req.Content.ReadAsStringAsync();

                List<LockOperationWithKey> lockOps = JsonSerializer.Deserialize<List<LockOperationWithKey>>(input);

                string guid = Guid.NewGuid().ToString();

                await client.StartNewAsync("UnLockOrchestration", guid, lockOps);

                return await client.WaitForCompletionOrCreateCheckStatusResponseAsync(req,
                                                                                   guid,
                                                                                   TimeSpan.FromSeconds(waitForResultSeconds is null
                                                                                   ? 5
                                                                                   : waitForResultSeconds.Value));



            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        ///// <summary>
        ///// This is used to check if there is a lock with DurableEntityClient
        ///// </summary>
        ///// <param name="lockName">Lock Id to lock on</param>
        ///// <returns>Lock read result</returns>
        [FunctionName("ReadLock")]
        public static async Task<HttpResponseMessage> ReadLock([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ReadLock/{LockName}/{LockType}/{LockId}")] HttpRequestMessage req,
                                                               [DurableClient] IDurableEntityClient client,
                                                               string lockName,
                                                               string lockType,
                                                               string lockId)
        {
            LockOperationResult read = await client.ExecuteRead(new LockOperationWithKey() { LockName = lockName, LockType = lockType, LockId = lockId });

            HttpResponseMessage resp = new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(read))
            };

            return resp;
        }

        ///// <summary>
        ///// This is used to check a list of locks if there is a lock with DurableEntityClient
        ///// </summary>
        ///// <returns>Lock read result list</returns>
        [FunctionName("ReadLocks")]
        public static async Task<HttpResponseMessage> ReadLocks([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ReadLocks")] HttpRequestMessage req,
                                                               [DurableClient] IDurableEntityClient client)
        {
            string strLockOp = JsonSerializer.Serialize(new LockOperationResult());

            string input = await req.Content.ReadAsStringAsync();

            List<LockOperationWithKey> lockOps = JsonSerializer.Deserialize<List<LockOperationWithKey>>(input);

            List<Task<LockOperationResult>> lockRes = new();
            List<LockOperationResult> lockResDto = new();

            foreach (LockOperationWithKey lockOp in lockOps)
            {
                lockRes.Add(client.ExecuteRead(lockOp));
            }

            HttpResponseMessage resp = new(HttpStatusCode.OK);

            while (lockRes.Count > 0)
            {
                Task<LockOperationResult> res = await Task.WhenAny(lockRes);
                lockResDto.Add(res.Result);
                lockRes.Remove(res);
            }

            resp.Content = new StringContent(JsonSerializer.Serialize(lockResDto));

            return resp;
        }

        /// <summary>
        /// Delete lock state with DurableClient
        /// </summary>
        /// <param name="req"></param>
        /// <param name="client"></param>
        /// <param name="lockName"></param>
        /// <param name="lockType"></param>
        /// <param name="lockId"></param>
        /// <param name="waitForResultSeconds"></param>
        /// <returns></returns>
        [FunctionName("DeleteLock")]
        public static async Task<HttpResponseMessage> DeleteLock([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "DeleteLock/{waitForResultSeconds:int?}")] HttpRequestMessage req,
                                                                 [DurableClient] IDurableOrchestrationClient client,
                                                                 int? waitForResultSeconds)
        {
            string input = await req.Content.ReadAsStringAsync();

            LockOperationWithKey lockOp = JsonSerializer.Deserialize<LockOperationWithKey>(input);

            string instanceId = await client.StartNewAsync("DeleteOrchestration", null, lockOp);

            return await client.WaitForCompletionOrCreateCheckStatusResponseAsync(req,
                                                                                   instanceId,
                                                                                   TimeSpan.FromSeconds(waitForResultSeconds is null
                                                                                   ? 5
                                                                                   : waitForResultSeconds.Value));
        }

        #endregion

        #region Orchestrations

        /// <summary>
        /// Delete orchestration with DurableOrchestrationContext
        /// </summary>
        /// <returns></returns>
        [Deterministic]
        [FunctionName("DeleteOrchestration")]
        public static async Task<int> DeleteOrchestration([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            LockOperationWithKey lockOp = context.GetInput<LockOperationWithKey>();

            EntityId entityId = new(lockOp.LockName, $"{lockOp.LockType}@{lockOp.LockId}");

            return await context.CallEntityAsync<int>(entityId, Constants.DeleteLock, (lockOp, lockOp.Key));
        }

        /// <summary>
        /// Lock orchestration with DurableOrchestrationContext
        /// </summary>
        /// <returns></returns>
        [Deterministic]
        [FunctionName("MainLockOrchestration")]
        public static async Task<LockResultResponse> MainLockOrchsestration([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            try
            {
                List<LockOperationWithKey> lockOps = JsonSerializer.Deserialize<List<LockOperationWithKey>>(context.GetInput<string>());

                List<Task<LockOperationResult>> lockResponses = new();
                List<Task<LockOperationResult>> unlockResponses;
                List<LockOperationResult> successLi = new();

                foreach (LockOperationWithKey lockOp in lockOps)
                {
                    lockResponses.Add(context.CallSubOrchestratorAsync<LockOperationResult>("SubLockOrchestration", context.NewGuid().ToString(), lockOp));
                }

                await Task.WhenAll(lockResponses);

                List<string> lockedItems = new();

                List<LockOperationResult> conflictsLi = new();

                foreach (Task<LockOperationResult> lockOp in lockResponses)
                {
                    if (lockOp.Result.Conflicted)
                    {
                        conflictsLi.Add(lockOp.Result);
                    }
                    else
                    {
                        successLi.Add(lockOp.Result);
                    }
                }

                // conflicts, rollback successfull locks
                if (conflictsLi.Count > 0)
                {
                    unlockResponses = new();

                    foreach (LockOperationResult lockOp in successLi)
                    {
                        unlockResponses.Add(context.CallSubOrchestratorAsync<LockOperationResult>("SubUnlockOrchestration", context.NewGuid().ToString(), lockOp));
                    }

                    while (unlockResponses.Count > 0)
                    {
                        Task<LockOperationResult> res = await Task.WhenAny(unlockResponses);

                        conflictsLi.Add(res.Result);

                        unlockResponses.Remove(res);
                    }

                    return new LockResultResponse()
                    {
                        HttpStatusCode = 409,
                        Locks = conflictsLi
                    };
                }

                return new LockResultResponse() { HttpStatusCode = 201, Locks = successLi };
            }
            catch (Exception ex)
            {
                return new LockResultResponse() { HttpStatusCode = 500 };
            }
        }

        /// <summary>
        /// Lock orchestration with DurableOrchestrationContext
        /// </summary>
        /// <returns></returns>
        [Deterministic]
        [FunctionName("UnLockOrchestration")]
        public static async Task<LockResultResponse> UnLockOrchestration([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            try
            {
                List<LockOperationWithKey> lockOps = context.GetInput<List<LockOperationWithKey>>();

                List<Task<LockOperationResult>> unlockResponses = new();

                List<LockOperationResult> resLi = new();

                foreach (LockOperationWithKey lockOp in lockOps)
                {
                    // call sub orch
                    unlockResponses.Add(context.CallSubOrchestratorAsync<LockOperationResult>("SubUnlockOrchestration", $"{lockOp.LockName}@{lockOp.LockType}@{lockOp.LockId}", lockOp));
                }

                while(unlockResponses.Count > 0)
                {
                    Task<LockOperationResult> res = await Task.WhenAny(unlockResponses);

                    if (res.Result != null)
                    {
                        resLi.Add(res.Result);
                    }

                    unlockResponses.Remove(res);
                }

                return new LockResultResponse() { HttpStatusCode = 200, Locks = resLi };
            }
            catch (Exception ex)
            {
                return new LockResultResponse() { HttpStatusCode = 500 };
            }
        }

        /// <summary>
        /// Lock orchestration with DurableOrchestrationContext
        /// </summary>
        /// <returns></returns>
        [Deterministic]
        [FunctionName("SubLockOrchestration")]
        public static async Task<LockOperationResult> SubLockOrchestration([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            LockOperationWithKey lockOp = context.GetInput<LockOperationWithKey>();

            return await context.LockOrchestration(Constants.Lock, lockOp);
        }

        /// <summary>
        /// Unlock orchestration with DurableOrchestrationContext
        /// </summary>
        /// <returns></returns>
        [Deterministic]
        [FunctionName("SubUnlockOrchestration")]
        public static async Task<LockOperationResult> SubUnlockOrchestration([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            LockOperationWithKey lockOp = context.GetInput<LockOperationWithKey>();

            return await context.LockOrchestration(Constants.UnLock, lockOp);
        }

        #endregion
    }
}
