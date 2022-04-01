#The elusive distributed lock is now a simple implementation thanks to Durable Functions. This micro-service Api Durable Function lock implementation is a simple to use but powerful locking implementation that already contain all the basic functinality that you need. Happy locking!

- Re-use the DurableLockApi and DurableLockLibrary class library to quickly and easily create your own Durable Locks.
- DurableLockApi class is a re-usable implementation for any type of lock in your system. Lock, unlock, read, and delete your lock types.
- Copy the DurableLockApi class and edit the value of const string LockType. Yes that is all that is needed to create your own new Durable lock types.
- You can either create a custom lock Api by modifying or copying CustomLockApi1.cs, or you can use GenericLockApi.cs which is geared to handle locks in a generic way.
- The following Api functionality is provided for both the custom and generic implementations:
  * lock
  * un-lock
  * read lock
  * get locks
  * delete lock
