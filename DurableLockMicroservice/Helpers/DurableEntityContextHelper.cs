using Durable.Lock.Models;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Durable.Lock.Api
{
    /// <summary>
    /// Generic Durable Lock functionality
    /// </summary>
    public static class DurableEntityContextHelper
    {
        #region DurableEntityContext helpers

        /// <summary>
        /// Generic re-usable lock for a shared class library
        /// </summary>
        /// <param name="ctx">DurableEntityContext</param>
        public static void CreateLock(this IDurableEntityContext ctx, string operartionName, LockOperationResult lockOpRes)
        {
            switch (ctx.OperationName)
            {
                case Constants.Lock:
                    {
                        LockState lockState = ctx.GetState<LockState>();
                        lockState = lockState is null ? new LockState() : lockState;

                        if (!lockState.IsLocked)
                        {
                            lockState.User = lockOpRes.User;
                            lockState.LockDate = lockOpRes.LockDate;
                            lockState.IsLocked = true;

                            ctx.SetState(lockState);
                        }

                        ctx.Return(lockState);

                        break;
                    }

                case Constants.UnLock:
                    {
                        LockState lockState = ctx.GetState<LockState>();

                        if(lockState is null)
                        {
                            ctx.Return(null);

                            break;
                        }

                        lockState.User = lockOpRes.User;
                        lockState.LockDate = lockOpRes.LockDate;
                        lockState.IsLocked = false;

                        ctx.SetState(lockState);
                        
                        ctx.Return(lockState);

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
    }
}