namespace DurableLockModels
{
    public class LockBase
    {
        public string User { get; set; }
    }

    public class LockState : LockBase
    {
        public bool IsLocked { get; set; }
        public DateTime LockDate { get; set; }
    }

    public class LockOperation : LockBase
    {
        public string LockName { get; set; }
        public string LockType { get; set; }
        public string LockId { get; set; }
    }

    public class LockOperationResult : LockOperation
    {
        public bool IsLocked { get; set; }
        public DateTime LockDate { get; set; }

        public bool Confilcted { get; set; }
    }

    public class LockResultResponse
    {
        public int HttpStatusCode { get; set; }
        public List<LockOperationResult> Locks { get; set; }
    }
}
