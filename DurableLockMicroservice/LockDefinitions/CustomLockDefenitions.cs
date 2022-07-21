using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Durable.Lock.Models;

namespace Durable.Lock.Api
{
    /// <summary>
    /// This class can be used to quickly create your own type of lock.
    /// Simply add new locks here or copy this class and change the LockType value to create a new type of lock.
    /// </summary>
    public static class CustomLockDefenitions
    {
        /// <summary>
        /// Generic lock with DurableEntityContext
        /// </summary>
        [FunctionName("GenericLock")]
        public static void GenericLock([EntityTrigger] IDurableEntityContext ctx)
            => ctx.CreateLock(Constants.Lock, ctx.GetInput<LockOperationResult>());
    }
}