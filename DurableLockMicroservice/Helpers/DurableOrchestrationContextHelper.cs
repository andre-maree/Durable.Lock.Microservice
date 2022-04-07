using Durable.Lock.Models;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;

namespace Durable.Lock.Api
{
    /// <summary>
    /// Generic Durable Lock functionality
    /// </summary>
    public static class DurableOrchestrationContextHelper
    {
        #region DurableOrchestrationContext helpers

        /// <summary>
        /// This calls back to the defined lock for this lock type in the client code
        /// </summary>
        /// <param name="context">DurableOrchestrationContext</param>
        /// <param name="lockType">This string value is the name of the type of lock</param>
        /// <returns>True for locked and false for unlocked</returns>
        public static async Task<LockOperationResult> LockOrchestration(this IDurableOrchestrationContext context, string operartionName, LockOperation lockOp)
        {
            //string operartionName = lockOp.IsLocked ? Constants.Lock : Constants.UnLock;

            EntityId entityId = new(lockOp.LockName, $"{lockOp.LockType}@{lockOp.LockId}");

            try
            {
                using (await context.LockAsync(entityId))
                {
                    LockOperationResult lockOperationResult = new LockOperationResult()
                    {
                        LockDate = context.CurrentUtcDateTime,
                        User = lockOp.User,
                        LockId = lockOp.LockId,
                        LockName = lockOp.LockName,
                        LockType = lockOp.LockType
                    };

                    LockState lockState = await context.CallEntityAsync<LockState>(entityId, operartionName, lockOperationResult);

                    if (lockState == null)
                    {
                        return null;
                    }
                    // conflict, set results to existing lock
                    if (lockState.LockDate != lockOperationResult.LockDate)
                    {
                        lockOperationResult.Confilcted = true;
                        lockOperationResult.LockDate = lockState.LockDate;
                        lockOperationResult.User = lockState.User;
                    }

                    lockOperationResult.IsLocked = lockState.IsLocked;

                    return lockOperationResult;
                };
            }
            catch(LockingRulesViolationException ex)
            {
                return new LockOperationResult()
                {
                    User = lockOp.User,
                    LockId = lockOp.LockId,
                    LockName = lockOp.LockName,
                    LockType = lockOp.LockType,
                    Confilcted = true
                };
            }
            catch (System.Exception ex)
            {
                return new LockOperationResult()
                {
                    User = lockOp.User,
                    LockId = lockOp.LockId,
                    LockName = lockOp.LockName,
                    LockType = lockOp.LockType,
                    Confilcted = true
                };
            }
        }

        #endregion
    }
}
