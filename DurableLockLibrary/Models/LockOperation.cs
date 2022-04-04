namespace DurableLockLibrary
{
    public class LockOperation
    {
        public string LockType { get; set; }
        public string LockId { get; set; }
        //public bool StayLocked { get; set; }
    }

    public class LockOperationResult : LockOperation
    {
        public bool IsLocked { get; set; }
    }

    public class LockResult
    {
        public int HttpStatusCode { get; set; }
        public List<LockOperation> Locks { get; set; }
    }
}
