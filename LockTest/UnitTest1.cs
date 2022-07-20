using Durable.Lock.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LockTest
{
    [TestClass]
    public class UnitTest1
    {
        private static HttpClient HttpClient = new HttpClient();

        [TestMethod]
        public async Task TestLoop()
        {
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    await TestConcurrency().ConfigureAwait(false);
                    await TestAll().ConfigureAwait(false);
                }

                Assert.IsTrue(true);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [TestMethod]
        public async Task TestConcurrency()
        {
            try
            {
                string lockType = "project";
                string lockName = "GenericLock";

                //get locks input list from post data
                List<LockOperation> lockOps = new()
                {
                    new LockOperation() { LockId = "a1", LockType = lockType, User = "user1", LockName = lockName }
                };

                HttpResponseMessage del1 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a1").ConfigureAwait(false);

                
                if (del1.StatusCode==System.Net.HttpStatusCode.Accepted)
                {
                    while (true)
                    {
                        HttpResponseMessage readres = await HttpClient.GetAsync($"http://localhost:7071/ReadLock/{lockName}/{lockType}/a1").ConfigureAwait(false);
                        var read = JsonSerializer.Deserialize<LockOperationResult>(await readres.Content.ReadAsStringAsync().ConfigureAwait(false));

                        if(read?.User is null)
                        {
                            break;
                        }
                    }
                }
                
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////
                var resLocks2 = HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/1000", lockOps);
                var resLocks3 = HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/1000", lockOps);

                LockResultResponse result = JsonSerializer.Deserialize<LockResultResponse>(await resLocks2.Result.Content.ReadAsStringAsync().ConfigureAwait(false));

                LockResultResponse result2 = JsonSerializer.Deserialize<LockResultResponse>(await resLocks3.Result.Content.ReadAsStringAsync().ConfigureAwait(false));

                Assert.IsTrue((result.HttpStatusCode == 201 && result2.HttpStatusCode == 409) || (result.HttpStatusCode == 409 && result2.HttpStatusCode == 201));
            }
            catch (Exception)
            {
                throw;
            }
        }

        [TestMethod]
        public async Task TestAll()
        {
            try
            {
                string lockType = "project";
                string lockName = "GenericLock";

                ////get locks input list from post data
                List<LockOperation> lockOps = new()
                {
                    new LockOperation() { LockId = "a1", LockType = lockType, User = "user1", LockName = lockName },
                    new LockOperation() { LockId = "a2", LockType = lockType, User = "user2", LockName = lockName },
                    new LockOperation() { LockId = "a3", LockType = lockType, User = "user3", LockName = lockName }
                };

                HttpResponseMessage del1 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a1").ConfigureAwait(false);
                HttpResponseMessage del2 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a2").ConfigureAwait(false);
                HttpResponseMessage del3 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a3").ConfigureAwait(false);

                Assert.IsTrue(del1.StatusCode == System.Net.HttpStatusCode.OK || del1.StatusCode == System.Net.HttpStatusCode.Accepted);
                Assert.IsTrue(del2.StatusCode == System.Net.HttpStatusCode.OK || del2.StatusCode == System.Net.HttpStatusCode.Accepted);
                Assert.IsTrue(del3.StatusCode == System.Net.HttpStatusCode.OK || del3.StatusCode == System.Net.HttpStatusCode.Accepted);


                //////////////////////////////////////////////////////////////////////////////////////////////////////////////
                HttpResponseMessage readres = await HttpClient.PostAsJsonAsync("http://localhost:7071/ReadLocks", lockOps).ConfigureAwait(false);
                var reads = JsonSerializer.Deserialize<List<LockOperationResult>>(await readres.Content.ReadAsStringAsync().ConfigureAwait(false));

                while(reads.FindAll(l => l.User == null).Count < 3)
                {
                    readres = await HttpClient.PostAsJsonAsync("http://localhost:7071/ReadLocks", lockOps).ConfigureAwait(false);
                    reads = JsonSerializer.Deserialize<List<LockOperationResult>>(await readres.Content.ReadAsStringAsync().ConfigureAwait(false));

                    await Task.Delay(1000);
                }
                
                Assert.IsTrue(reads.FindAll(l => l.User == null).Count == 3);


                ////////////////////////////////////////////////////////////////////////////////////////////////////////////
                HttpResponseMessage resLocks = await HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/150", lockOps).ConfigureAwait(false);
                LockResultResponse result = JsonSerializer.Deserialize<LockResultResponse>(await resLocks.Content.ReadAsStringAsync().ConfigureAwait(false));

                Assert.IsTrue(result.HttpStatusCode == 201);
                Assert.IsTrue(result.Locks.FindAll(l => l.IsLocked == true
                                                        && (l.User.Equals("user1") || l.User.Equals("user2") || l.User.Equals("user3"))
                                                        && (l.LockId.Equals("a1") || l.LockId.Equals("a2") || l.LockId.Equals("a3"))).Count == 3);


                ///////////////////////////////////////////////////////////////////////////////////////
                resLocks = await HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/150", lockOps).ConfigureAwait(false);
                result = JsonSerializer.Deserialize<LockResultResponse>(await resLocks.Content.ReadAsStringAsync().ConfigureAwait(false));

                Assert.IsTrue(result.HttpStatusCode == 409);
                Assert.IsTrue(result.Locks.FindAll(l => l.IsLocked == true
                                                        && l.Conflicted == true
                                                        && (l.User.Equals("user1") || l.User.Equals("user2") || l.User.Equals("user3"))
                                                        && (l.LockId.Equals("a1") || l.LockId.Equals("a2") || l.LockId.Equals("a3"))).Count == 3);


                ///////////////////////////////////////////////////////////////////////////////////////
                readres = await HttpClient.PostAsJsonAsync("http://localhost:7071/ReadLocks", lockOps).ConfigureAwait(false);
                reads = JsonSerializer.Deserialize<List<LockOperationResult>>(await readres.Content.ReadAsStringAsync());

                Assert.IsTrue(reads.FindAll(l => !string.IsNullOrWhiteSpace(l.User)).Count == 3);


                ///////////////////////////////////////////////////////////////////////////////////////////
                resLocks = await HttpClient.PostAsJsonAsync("http://localhost:7071/unLock/150", lockOps).ConfigureAwait(false);
                result = JsonSerializer.Deserialize<LockResultResponse>(await resLocks.Content.ReadAsStringAsync().ConfigureAwait(false));

                Assert.IsTrue(result.HttpStatusCode == 200);
                Assert.IsTrue(result.Locks.FindAll(l => l.IsLocked == false
                                                        && l.Conflicted == false
                                                        && (l.User.Equals("user1") || l.User.Equals("user2") || l.User.Equals("user3"))
                                                        && (l.LockId.Equals("a1") || l.LockId.Equals("a2") || l.LockId.Equals("a3"))).Count == 3);


                //////////////////////////////////////////////////////////////////////////////////////
                readres = await HttpClient.PostAsJsonAsync("http://localhost:7071/ReadLocks", lockOps).ConfigureAwait(false);
                reads = JsonSerializer.Deserialize<List<LockOperationResult>>(await readres.Content.ReadAsStringAsync().ConfigureAwait(false));

                Assert.IsTrue(reads.FindAll(l => !string.IsNullOrWhiteSpace(l.User)).Count == 3);


                // lock 1 //////////////////////////////////////////////////////////////////////////////////
                resLocks = await HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/150", lockOps.FindAll(l => l.LockId.Equals("a1"))).ConfigureAwait(false);
                result = JsonSerializer.Deserialize<LockResultResponse>(await resLocks.Content.ReadAsStringAsync().ConfigureAwait(false));

                Assert.IsTrue(result.HttpStatusCode == 201);
                Assert.IsTrue(result.Locks.FindAll(l => l.IsLocked == true
                                                        && l.User.Equals("user1")
                                                        && l.LockId.Equals("a1")).Count == 1);


                // lock all to create conflict ///////////////////////////////////////////////////////
                resLocks = await HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/150", lockOps).ConfigureAwait(false);
                result = JsonSerializer.Deserialize<LockResultResponse>(await resLocks.Content.ReadAsStringAsync().ConfigureAwait(false));

                Assert.IsTrue(result.HttpStatusCode == 409);
                Assert.IsTrue(result.Locks.FindAll(l => l.IsLocked == true
                                                        && l.Conflicted
                                                        && l.User.Equals("user1")
                                                        && l.LockId.Equals("a1")).Count == 1);

                Assert.IsTrue(result.Locks.FindAll(l => l.IsLocked == false
                                                        && l.Conflicted == false
                                                        && (l.User.Equals("user2") || l.User.Equals("user3"))
                                                        && (l.LockId.Equals("a2") || l.LockId.Equals("a3"))).Count == 2);


                //////////////////////////////////////////////////////////////////////////////////////
                readres = await HttpClient.PostAsJsonAsync("http://localhost:7071/ReadLocks", lockOps).ConfigureAwait(false);
                reads = JsonSerializer.Deserialize<List<LockOperationResult>>(await readres.Content.ReadAsStringAsync().ConfigureAwait(false));

                Assert.IsTrue(reads.FindAll(l => !string.IsNullOrWhiteSpace(l.User)).Count == 3);

                del1 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a1").ConfigureAwait(false);
                del2 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a2").ConfigureAwait(false);
                del3 = await HttpClient.GetAsync($"http://localhost:7071/DeleteLock/{lockName}/{lockType}/a3").ConfigureAwait(false);

                Assert.IsTrue(del1.StatusCode == System.Net.HttpStatusCode.OK || del1.StatusCode == System.Net.HttpStatusCode.Accepted);
                Assert.IsTrue(del2.StatusCode == System.Net.HttpStatusCode.OK || del2.StatusCode == System.Net.HttpStatusCode.Accepted);
                Assert.IsTrue(del3.StatusCode == System.Net.HttpStatusCode.OK || del3.StatusCode == System.Net.HttpStatusCode.Accepted);
            }
            catch (Exception )
            {
                throw;
            }
        }
    }
}