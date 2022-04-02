namespace DurableLockLibrary
{
    public class LockOperation
    {
        public string LockType { get; set; }
        public string LockId { get; set; }
        public bool PermanentLock { get; set; }
    }

    public class HttpLockOperation : LockOperation
    {
        public HttpResponseMessage HttpLockResponse { get; set; }
    }
}
