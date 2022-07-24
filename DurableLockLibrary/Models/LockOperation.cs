namespace Durable.Lock.Models
{
    public class LockBase
    {
        public string User { get; set; }
    }

    public class LockState : LockBase
    {
        public bool IsLocked { get; set; }
        public DateTime LockDate { get; set; }
        public string LockKey { get; set; }
    }

    public class LockOperation : LockBase
    {
        public string LockName { get; set; }
        public string LockType { get; set; }
        public string LockId { get; set; }
        //public string Key { get; set; }
    }

    // This used only when it is needed to lock with a key, cannot be read, input to set only
    public class LockOperationWithKey : LockOperation
    {
        public string Key { get; set; }
    }

    public class LockOperationResult : LockOperation
    {
        public bool IsLocked { get; set; }
        public DateTime LockDate { get; set; }

        public bool Conflicted { get; set; }
    }

    public class LockResultResponse
    {
        public int HttpStatusCode { get; set; }
        public List<LockOperationResult> Locks { get; set; }
    }
}
