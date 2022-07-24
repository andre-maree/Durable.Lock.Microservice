using Durable.Lock.Models;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;

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
        public static void CreateLock(this IDurableEntityContext ctx, LockOperationResult lockOpRes)
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

                            //if (!string.IsNullOrWhiteSpace(tuple.lockKey))
                            //{
                            lockState.LockKey = lockOpRes.Key;
                            //}

                            ctx.SetState(lockState);
                        }

                        ctx.Return(lockState);

                        break;
                    }

                case Constants.UnLock:
                    {
                        LockState lockState = ctx.GetState<LockState>();

                        if ((lockState is null) || LockKeyFail(lockOpRes, lockState))
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
                        LockState lockState = ctx.GetState<LockState>();

                        if (lockState is null)
                        {
                            ctx.Return(404);
                            break;
                        }

                        if (LockKeyFail(lockOpRes, lockState))
                        {
                            ctx.Return(409);
                            break;
                        }

                        ctx.DeleteState();

                        ctx.Return(200);

                        break;
                    }
            }
        }

        private static bool LockKeyFail(LockOperationResult lockOpRes, LockState lockState)
        {
            if (!string.IsNullOrWhiteSpace(lockState.LockKey))
            {
                if (lockState.LockKey.Equals(lockOpRes.Key))
                {
                    return false;
                }

                if (lockOpRes.Key != null && lockOpRes.Key.Equals(Environment.GetEnvironmentVariable("MasterLockKey")))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        #endregion
    }
}