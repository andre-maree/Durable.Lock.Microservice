using DurableLockLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        public async Task TestMethod1()
        {
            string lockType = "project";

            ////get locks input list from post data
            List<HttpLockOperation> lockOps = new()
            {
                new HttpLockOperation() { LockId = "a1", LockType = lockType },
                new HttpLockOperation() { LockId = "a2", LockType = lockType, StayLocked = true },
                new HttpLockOperation() { LockId = "a3", LockType = lockType }
            };

            var res = await HttpClient.PostAsJsonAsync("http://localhost:7071/Lock/project/5/55", lockOps);

            List<string> result = JsonSerializer.Deserialize<List<string>>(await res.Content.ReadAsStringAsync());

            foreach(var loc in result)
            {
                res = await HttpClient.GetAsync(loc);
            }
        }
    }
}