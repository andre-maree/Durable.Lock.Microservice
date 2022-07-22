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
        public static void CreateLock(this IDurableEntityContext ctx, (LockOperationResult lockOpRes, string lockKey) tuple)
        {
            switch (ctx.OperationName)
            {
                case Constants.Lock:
                    {
                        LockState lockState = ctx.GetState<LockState>();
                        lockState = lockState is null ? new LockState() : lockState;

                        if (!lockState.IsLocked)
                        {
                            lockState.User = tuple.lockOpRes.User;
                            lockState.LockDate = tuple.lockOpRes.LockDate;
                            lockState.IsLocked = true;

                            //if (!string.IsNullOrWhiteSpace(tuple.lockKey))
                            //{
                                lockState.LockKey = tuple.lockKey;
                            //}

                            ctx.SetState(lockState);
                        }

                        ctx.Return(lockState);

                        break;
                    }

                case Constants.UnLock:
                    {
                        LockState lockState = ctx.GetState<LockState>();

                        if ((lockState is null) || (!string.IsNullOrWhiteSpace(lockState.LockKey) && !lockState.LockKey.Equals(tuple.lockKey)))
                        {
                            ctx.Return(null);

                            break;
                        }

                        //if (!string.IsNullOrWhiteSpace(lockState.LockKey) && !lockState.LockKey.Equals(lockKey))
                        //{

                        //}

                        lockState.User = tuple.lockOpRes.User;
                        lockState.LockDate = tuple.lockOpRes.LockDate;
                        lockState.IsLocked = false;

                        ctx.SetState(lockState);

                        ctx.Return(lockState);

                        break;
                    }

                case Constants.DeleteLock:
                    {
                        LockState lockState = ctx.GetState<LockState>(); 
                        
                        if ((lockState is null) || (!string.IsNullOrWhiteSpace(lockState.LockKey) && !lockState.LockKey.Equals(tuple.lockKey)))
                        {
                            ctx.Return(null);

                            break;
                        }

                        ctx.DeleteState();

                        break;
                    }
            }
        }

        #endregion
    }
}