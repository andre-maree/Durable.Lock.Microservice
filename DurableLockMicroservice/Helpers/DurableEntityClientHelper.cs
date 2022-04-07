using Durable.Lock.Models;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;

namespace Durable.Lock.Api
{
    /// <summary>
    /// Generic Durable Lock functionality
    /// </summary>
    public static class DurableEntityClientHelper
    {
        #region DurableEntityClient helpers

        public static async Task<LockOperationResult> ExecuteRead(this IDurableEntityClient client, LockOperation lockOp)
        {
            return await ReadDurableLock(client, lockOp);
        }

        /// <summary>
        /// Read the lock state
        /// </summary>
        /// <param name="client">DurableEntityClient</param>
        /// <param name="lockType">This string value is the name of the type of lock</param>
        /// <param name="lockId">This string value is the key for the lock type</param>
        /// <returns>200 and true for locked and false for unlocked</returns>
        public static async Task<LockOperationResult> ReadDurableLock(IDurableEntityClient client, LockOperation lockOp)
        {
            EntityId entId = new(lockOp.LockName, $"{lockOp.LockType}@{lockOp.LockId}");

            EntityStateResponse<LockState> lockState = await client.ReadEntityStateAsync<LockState>(entId);

            if(!lockState.EntityExists)
            {
                return new LockOperationResult()
                {
                    LockName = lockOp.LockName,
                    LockType = lockOp.LockType,
                    LockId = lockOp.LockId
                };
            }

            return new LockOperationResult() { 
                IsLocked = lockState.EntityState.IsLocked, 
                LockId = lockOp.LockId, 
                LockType = lockOp.LockType,
                LockName = lockOp.LockName,
                LockDate = lockState.EntityState.LockDate,
                User = lockState.EntityState.User
            };
        }

        #endregion
    }
}
