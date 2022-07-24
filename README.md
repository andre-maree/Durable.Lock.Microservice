### The end of all locking code has arrived:

#### The elusive distributed lock is now a simple implementation thanks to Durable.Lock.Microservice. This micro-service Api Durable Function lock implementation is simple to use but powerful and contains all the basic functionality that you need.

- Re-use the DurableLockApi and DurableLockLibrary class library to quickly and easily create your own Durable Locks.
- DurableLockApi class is a re-usable implementation for any type of lock in your system. Lock, unlock, read, and delete your lock types.
- Copy the DurableLockApi class and edit the value of const string LockType. That is all that is needed to create your own new Durable lock types.
- You can either create a custom lock Api by modifying or copying CustomLockDefenitions.cs, this is where locks are defined.
- The following Api functionality is provided for both the custom and generic implementations:
  * lock:
         - Pass in a list of locks that must be locked together, if any lock fails then all locks passed in will roll back and fail. All or no locks will succeed.
         - Lock with a key to only allow unlock and delete with the key.
         - Override an unlock or delete of a lock with a set key by using the master key. 
  * un-lock
         - Prevent an unlock by setting a lock key.
         - Override an unlock of a lock with a key by unlocking the master key.
  * read lock
  * get locks
  * delete lock
         - Prevent a delete by setting a lock key.
         - Override a lock with a key by deleting with the master key.
