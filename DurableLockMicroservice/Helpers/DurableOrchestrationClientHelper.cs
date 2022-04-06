using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Net;
using System.Text.Json;

namespace DurableLockApi
{
    /// <summary>
    /// Generic Durable Lock functionality
    /// </summary>
    public static class DurableOrchestrationClientHelper
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
        //public static async Task<HttpResponseMessage> DurableLockOrchestrationStart(this IDurableOrchestrationClient client,
        //                                                                          HttpRequestMessage req,
        //                                                                          string orchestrationName,
        //                                                                          string lockType,
        //                                                                          string lockId,
        //                                                                          int? waitForResultSeconds,
        //                                                                          string opName,
        //                                                                          bool genericMode)
        //{
        //    string lockname = $"{lockType}@{lockId}";

        //    await client.StartNewAsync(orchestrationName, lockname, opName);

        //    using HttpResponseMessage? orchResponse = await client.WaitForCompletionOrCreateCheckStatusResponseAsync(req,
        //                                                                       lockname,
        //                                                                       TimeSpan.FromSeconds(waitForResultSeconds is null
        //                                                                       ? 5
        //                                                                       : waitForResultSeconds.Value));
        //    HttpResponseMessage? respsone;

        //    if (orchResponse.StatusCode == HttpStatusCode.OK)
        //    {
        //        string result = await orchResponse.Content.ReadAsStringAsync();

        //        // lock success
        //        if (result.Equals("true", StringComparison.OrdinalIgnoreCase))
        //        {
        //            string sepa = genericMode ? "/" : "";

        //            respsone = new(HttpStatusCode.Created)
        //            {
        //                Content = new StringContent($"http://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}/unlock{sepa}{lockType}/{lockId}")
        //            };
        //        }
        //        else // lock conflict for lock or OK for successful unlock
        //        {
        //            respsone = opName.Equals("lock") ? new(HttpStatusCode.Conflict) : new(HttpStatusCode.OK);
        //        }
        //    }
        //    else if (orchResponse.StatusCode == HttpStatusCode.Accepted)
        //    {
        //        respsone = new(HttpStatusCode.Accepted)
        //        {
        //            Content = new StringContent(orchResponse.Headers.Location.AbsoluteUri)
        //        };
        //    }
        //    else
        //    {
        //        return orchResponse;
        //    }

        //    return respsone;
        //}

        /// <summary>
        /// Get all locks per lock type or omit locktype to get all locks accross type
        /// </summary>
        /// <param name="client">DurableClient</param>
        /// <param name="lockType">This string value is the name of the type of lock</param>
        /// <returns>200</returns>
        //public static async Task<HttpResponseMessage> GetDurableLocks(this IDurableClient client, string lockName, string lockType = "")
        //{
        //    Dictionary<string, bool> result = new();
        //    EntityQueryResult? res = null;

        //    using (CancellationTokenSource cts = new())
        //    {
        //        res = await client.ListEntitiesAsync(new EntityQuery()
        //        {
        //            PageSize = 99999999,
        //            EntityName = !string.IsNullOrWhiteSpace(lockName) ? lockName + "Lock" : "",
        //            FetchState = true
        //        }, cts.Token);
        //    }

        //    if (!string.IsNullOrWhiteSpace(lockType))
        //    {
        //        foreach (DurableEntityStatus? rr in res.Entities.Where(e => e.EntityId.EntityKey.StartsWith(lockType + "@")))
        //        {
        //            result.Add(rr.EntityId.EntityKey, (bool)rr.State);
        //        }
        //    }
        //    else
        //    {
        //        foreach (DurableEntityStatus? rr in res.Entities)
        //        {
        //            result.Add(rr.EntityId.EntityKey, (bool)rr.State);
        //        }
        //    }

        //    StringContent? content = new(JsonSerializer.Serialize(result));

        //    return new HttpResponseMessage(HttpStatusCode.OK)
        //    {
        //        Content = content
        //    };
        //}

        #endregion
    }
}
