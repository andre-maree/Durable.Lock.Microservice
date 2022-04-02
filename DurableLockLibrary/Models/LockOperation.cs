namespace DurableLockLibrary
{
    public class LockOperation
    {
        public string LockType { get; set; }
        public string LockId { get; set; }
        public bool StayLocked { get; set; }
    }

    public class HttpLockOperation : LockOperation
    {
        public HttpResponseMessage HttpLockResponse { get; set; }
    }
}
