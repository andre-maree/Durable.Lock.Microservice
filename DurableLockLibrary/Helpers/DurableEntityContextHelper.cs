using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace DurableLockLibrary
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
    }
}
