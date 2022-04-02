using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace DurableLockLibrary
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
        public static async Task<bool> LockOrchestration(this IDurableOrchestrationContext context, string lockType)
        {
            string operartionName = context.GetInput<string>();

            EntityId entityId = new(lockType, context.InstanceId);

            return await context.CallEntityAsync<bool>(entityId, operartionName);
        }

        #endregion
    }
}
