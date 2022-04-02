using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Net;
using System.Text.Json;

namespace DurableLockLibrary
{
    /// <summary>
    /// Generic Durable Lock functionality
    /// </summary>
    public static class DurableLockHelper
    {
        #region DurableClient helpers

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
        public static async Task<HttpResponseMessage> DurableLockOrchestrationStart(this IDurableClient client,
                                                                                  HttpRequestMessage req,
                                                                                  string orchestrationName,
                                                                                  string lockType,
                                                                                  string lockId,
                                                                                  int? waitForResultSeconds,
                                                                                  string opName)
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
                    respsone = new(HttpStatusCode.Created)
                    {
                        Content = new StringContent($"http://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}/unlock/{lockType}/{lockId}")
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
        public static async Task<HttpResponseMessage> DeleteDurableLock(this IDurableClient client, string lockName, string lockType, string lockId)
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

        #region DurableEntityClient helpers

        /// <summary>
        /// Read the lock state
        /// </summary>
        /// <param name="client">DurableEntityClient</param>
        /// <param name="lockType">This string value is the name of the type of lock</param>
        /// <param name="lockId">This string value is the key for the lock type</param>
        /// <returns>200 and true for locked and false for unlocked</returns>
        public static async Task<HttpResponseMessage> ReadDurableLock(this IDurableEntityClient client, string entityId, string entityKey)
        {
            EntityId entId = new(entityId, entityKey);

            EntityStateResponse<bool> IsLocked = await client.ReadEntityStateAsync<bool>(entId);

            HttpResponseMessage respsone;

            if (IsLocked.EntityState)
            {
                respsone = new HttpResponseMessage(HttpStatusCode.Locked);
            }
            else
            {
                respsone = new HttpResponseMessage(HttpStatusCode.OK);
            }

            respsone.Content = new StringContent(IsLocked.EntityState.ToString());

            return respsone;
        }

        #endregion

        #region DurableEntityContext helpers

        /// <summary>
        /// Generic re-usable lock for a shared class library
        /// </summary>
        /// <param name="ctx">DurableEntityContext</param>
        public static void CreateLock(this IDurableEntityContext ctx)
        {
            switch (ctx.OperationName)
            {
                case Constants.Lock:
                    {
                        bool isLocked = ctx.GetState<bool>();

                        if (!isLocked)
                        {
                            ctx.SetState(true);
                        }

                        ctx.Return(!isLocked);

                        break;
                    }

                case Constants.UnLock:
                    {
                        ctx.SetState(false);

                        ctx.Return(ctx.GetState<bool>());

                        break;
                    }

                case Constants.DeleteLock:
                    {
                        ctx.DeleteState();

                        break;
                    }
            }
        }

        #endregion

        #region DurableOrchestrationContext helpers

        /// <summary>
        /// This calls back to the defined lock for this lock type in the client code
        /// </summary>
        /// <param name="context">DurableOrchestrationContext</param>
        /// <param name="lockType">This string value is the name of the type of lock</param>
        /// <returns>True for locked and false for unlocked</returns>
        public static async Task<bool> LockOrchestration(this IDurableOrchestrationContext context, string lockType)
        {
            string operartionName = context.GetInput<string>();

            EntityId entityId = new(lockType, context.InstanceId);

            return await context.CallEntityAsync<bool>(entityId, operartionName);
        }

        #endregion
    }
}
