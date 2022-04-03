using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Net;
using System.Text.Json;

namespace DurableLockLibrary
{
    /// <summary>
    /// Generic Durable Lock functionality
    /// </summary>
    public static class DurableOrchestrationClientHelper
    {
        #region DurableClient helpers

        //public static async Task<HttpResponseMessage> ExcecuteLock(this IDurableOrchestrationClient client,
        //                                                           HttpRequestMessage req,
        //                                                           string orchestratioName,
        //                                                           string lockName,
        //                                                           string lockType,
        //                                                           string lockId,
        //                                                           int? waitForResultSeconds,
        //                                                           bool genericMode)
        //{
        //    //    ////get locks input list from post data
        //    //List<HttpLockOperation> lockOps = new();
        ////    {
        ////        new HttpLockOperation() { LockId = "a1", LockType = lockType },
        ////        new HttpLockOperation() { LockId = "a2", LockType = lockType, StayLocked = true },
        ////        new HttpLockOperation() { LockId = "a3", LockType = lockType }
        ////    };

        //    string content = await req.Content.ReadAsStringAsync();

        //    var mainLockOp = new HttpLockOperation()
        //    {
        //        LockId = lockId,
        //        LockType = lockType
        //    };

        //    // no other locks posted, just main lock
        //    if (string.IsNullOrWhiteSpace(content))
        //    {
        //        var result = await client.ExecuteLock(req, orchestratioName, waitForResultSeconds, mainLockOp, Constants.Lock, genericMode);

        //        return result.HttpLockResponse;
        //    }

        //    /////////////////////////////// reads //////////////////////////////////
        //    // 1st just do quick reads on all locks, no need to try do lock orchestrations if a read returns locked 423, exit

        //    List<HttpLockOperation> lockOps = JsonSerializer.Deserialize<List<HttpLockOperation>>(content);

        //    List<Task<HttpResponseMessage>> readTasks = new();

        //    lockOps.Add(mainLockOp);

        //    foreach (HttpLockOperation loclOp in lockOps)
        //    {
        //        //readTasks.Add(DurableEntityClientHelper.ReadDurableLock(new IDurableOrchestrationClient(), lockName, $"{loclOp.LockType}@{loclOp.LockId}"));
        //    }

        //    int readCount = 0;

        //    while (readCount < readTasks.Count)
        //    {
        //        Task<HttpResponseMessage> readTask = await Task.WhenAny<HttpResponseMessage>(readTasks);

        //        // if any read says locked then bail out
        //        if (readTask.Result.StatusCode != HttpStatusCode.OK)
        //        {
        //            return new HttpResponseMessage(readTask.Result.StatusCode);
        //        }

        //        readCount++;
        //    }

        //    /////////////////////// all reads said unlocked, continue below /////////////////////////////////
        //    // now do lock orhestrations

        //    List<Task<HttpLockOperation>> lockResponses = new();
        //    List<Task<HttpLockOperation>> unlockResponses = new();
        //    List<HttpLockOperation> lockedLi = new();

        //    foreach (HttpLockOperation lockOp in lockOps)
        //    {
        //        // call sub orch
        //        lockResponses.Add(client.ExecuteLock(req, orchestratioName, waitForResultSeconds, lockOp, Constants.Lock, genericMode));
        //    }

        //    bool hasConflict = false;

        //    while (lockResponses.Count > 0)
        //    {
        //        Task<HttpLockOperation> lockOpTask = await Task.WhenAny<HttpLockOperation>(lockResponses);

        //        HttpLockOperation lockOp = lockOpTask.Result;

        //        lockResponses.Remove(lockOpTask);

        //        if (lockOp.HttpLockResponse.StatusCode == HttpStatusCode.Created)
        //        {
        //            if (hasConflict)
        //            {
        //                unlockResponses.Add(client.ExecuteLock(req, orchestratioName, waitForResultSeconds, lockOp, Constants.UnLock, genericMode));
        //            }
        //            else
        //            {
        //                lockedLi.Add(lockOp);
        //            }
        //        }
        //        // conflict check
        //        else if (lockOp.HttpLockResponse.StatusCode == HttpStatusCode.Conflict && !hasConflict)
        //        {
        //            hasConflict = true;

        //            // conflict found, wait for all locks and then unlock
        //            foreach (HttpLockOperation locked in lockedLi)
        //            {
        //                unlockResponses.Add(client.ExecuteLock(req, orchestratioName, waitForResultSeconds, lockOp, Constants.UnLock, genericMode));
        //            }
        //        }
        //    }

        //    if (hasConflict) // if a conflict was found, wait for the unlocks
        //    {
        //        await Task.WhenAll<HttpLockOperation>(unlockResponses);

        //        return new HttpResponseMessage(HttpStatusCode.Conflict);
        //    }

        //    foreach (HttpLockOperation lockOp in lockedLi.FindAll(l => !l.StayLocked))
        //    {
        //        unlockResponses.Add(client.ExecuteLock(req, orchestratioName, waitForResultSeconds, lockOp, Constants.UnLock, genericMode));
        //    }

        //    await Task.WhenAll<HttpLockOperation>(unlockResponses);

        //    List<string> li = new();

        //    foreach(var res in lockedLi)
        //    {
        //        if (res.HttpLockResponse.StatusCode == HttpStatusCode.Created)
        //        {
        //            li.Add(await res.HttpLockResponse.Content.ReadAsStringAsync());
        //        }
        //    }

        //    return new HttpResponseMessage(HttpStatusCode.Created)
        //    {
        //        Content = new StringContent(JsonSerializer.Serialize(li))
        //    };
        //}

        public static async Task<HttpLockOperation> ExecuteLock(this IDurableOrchestrationClient client,
                                                                HttpRequestMessage req,
                                                                string orchestratioName,
                                                                int? waitForResultSeconds,
                                                                HttpLockOperation lockOp,
                                                                string lockOperstion,
                                                                bool genericMode)
        {
            lockOp.HttpLockResponse = await client.DurableLockOrchestrationStart(req,
                                                               orchestratioName,
                                                               lockOp.LockType,
                                                               lockOp.LockId,
                                                               waitForResultSeconds,
                                                               lockOperstion,
                                                               genericMode);

            return lockOp;
        }

        /// <summary>
        /// Starts the Durable Lock orchestration from the DurableClient
        /// </summary>
        /// <param name="client">DurableClient</param>
        /// <param name="req">HttpRequest</param>
        /// <param name="orchestrationName"></param>
        /// <param name="lockType">This string value is the name of the type of lock</param>
        /// <param name="lockId">This string value is the key for the lock type</param>
        /// <param name="waitForResultSeconds">Specify how long to wait for a result before a 202 is returned,
        ///                                    default to 5 seconds if ommited</param>
        /// <param name="opName">Constants for this is in this class above, can be "lock", "unlock" or "delete"</param>
        /// <returns>200, or 202 if it takes longer than waitForResultSeconds, default is 5 seconds if omitted</returns>
        public static async Task<HttpResponseMessage> DurableLockOrchestrationStart(this IDurableOrchestrationClient client,
                                                                                  HttpRequestMessage req,
                                                                                  string orchestrationName,
                                                                                  string lockType,
                                                                                  string lockId,
                                                                                  int? waitForResultSeconds,
                                                                                  string opName,
                                                                                  bool genericMode)
        {
            string lockname = $"{lockType}@{lockId}";

            await client.StartNewAsync(orchestrationName, lockname, opName);

            using HttpResponseMessage? orchResponse = await client.WaitForCompletionOrCreateCheckStatusResponseAsync(req,
                                                                               lockname,
                                                                               TimeSpan.FromSeconds(waitForResultSeconds is null
                                                                               ? 5
                                                                               : waitForResultSeconds.Value));
            HttpResponseMessage? respsone;

            if (orchResponse.StatusCode == HttpStatusCode.OK)
            {
                string result = await orchResponse.Content.ReadAsStringAsync();

                // lock success
                if (result.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    string sepa = genericMode ? "/" : "";

                    respsone = new(HttpStatusCode.Created)
                    {
                        Content = new StringContent($"http://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}/unlock{sepa}{lockType}/{lockId}")
                    };
                }
                else // lock conflict for lock or OK for successful unlock
                {
                    respsone = opName.Equals("lock") ? new(HttpStatusCode.Conflict) : new(HttpStatusCode.OK);
                }
            }
            else if (orchResponse.StatusCode == HttpStatusCode.Accepted)
            {
                respsone = new(HttpStatusCode.Accepted)
                {
                    Content = new StringContent(orchResponse.Headers.Location.AbsoluteUri)
                };
            }
            else
            {
                return orchResponse;
            }

            return respsone;
        }

        /// <summary>
        /// Delete a lock for data cleanup
        /// </summary>
        /// <param name="client">DurableClient</param>
        /// <param name="lockType">This string value is the name of the type of lock</param>
        /// <param name="lockId">This string value is the key for the lock type</param>
        /// <returns>200</returns>
        public static async Task<HttpResponseMessage> DeleteDurableLock(this IDurableEntityClient client, string lockName, string lockType, string lockId)
        {
            EntityId entityId = new(lockName, $"{lockType}@{lockId}");

            await client.SignalEntityAsync(entityId, Constants.DeleteLock);

            return new(HttpStatusCode.OK);
        }

        /// <summary>
        /// Get all locks per lock type or omit locktype to get all locks accross type
        /// </summary>
        /// <param name="client">DurableClient</param>
        /// <param name="lockType">This string value is the name of the type of lock</param>
        /// <returns>200</returns>
        public static async Task<HttpResponseMessage> GetDurableLocks(this IDurableClient client, string lockName, string lockType = "")
        {
            Dictionary<string, bool> result = new();
            EntityQueryResult? res = null;

            using (CancellationTokenSource cts = new())
            {
                res = await client.ListEntitiesAsync(new EntityQuery()
                {
                    PageSize = 99999999,
                    EntityName = !string.IsNullOrWhiteSpace(lockName) ? lockName + "Lock" : "",
                    FetchState = true
                }, cts.Token);
            }

            if (!string.IsNullOrWhiteSpace(lockType))
            {
                foreach (DurableEntityStatus? rr in res.Entities.Where(e => e.EntityId.EntityKey.StartsWith(lockType + "@")))
                {
                    result.Add(rr.EntityId.EntityKey, (bool)rr.State);
                }
            }
            else
            {
                foreach (DurableEntityStatus? rr in res.Entities)
                {
                    result.Add(rr.EntityId.EntityKey, (bool)rr.State);
                }
            }

            StringContent? content = new(JsonSerializer.Serialize(result));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };
        }

        #endregion
    }
}
