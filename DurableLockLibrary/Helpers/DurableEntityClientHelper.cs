using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Net;

namespace DurableLockLibrary
{
    /// <summary>
    /// Generic Durable Lock functionality
    /// </summary>
    public static class DurableEntityClientHelper
    {
        #region DurableEntityClient helpers

        public static async Task<HttpLockOperation> ExecuteRead(this IDurableEntityClient client, string lockName, HttpLockOperation lockOp)
        {
            lockOp.HttpLockResponse = await client.ReadDurableLock(lockName, $"{lockOp.LockType}@{lockOp.LockId}");

            return lockOp;
        }

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
    }
}
