using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DurableLockLibrary;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace DurableLockApi.SharedLockApi
{
    public static class SharedLockApi
    {
        /// <summary>
        /// Get all locks
        /// </summary>
        /// <returns></returns>
        [FunctionName("GetLocks")]
        public static async Task<HttpResponseMessage> GetLocks([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetLocks/{Generic?}/{LockType?}")] HttpRequestMessage req,
                                                               [DurableClient] IDurableClient client,
                                                               string generic,
                                                               string lockType)
            => await client.GetDurableLocks(generic, lockType);

        /// <summary>
        /// This is used to read locks with DurableEntityClient
        /// </summary>
        /// <returns></returns>
        [FunctionName("ReadLocks")]
        public static async Task<HttpResponseMessage> ReadLocks([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ReadLocks/{LockName}/{RespondWhen1stLockFound?}")] HttpRequestMessage req,
                                                               [DurableClient] IDurableEntityClient client,
                                                               string lockName,
                                                               bool RespondWhen1stLockFound)
        {
            try
            {
                string content = await req.Content.ReadAsStringAsync();

                List<HttpLockOperation> lockOps = JsonSerializer.Deserialize<List<HttpLockOperation>>(content);

                List<Task<HttpLockOperation>> readTasks = new();

                foreach (HttpLockOperation lockOp in lockOps)
                {
                    readTasks.Add(client.ExecuteRead(lockName, lockOp));
                }

                int readCount = 0;

                if (RespondWhen1stLockFound)
                {
                    while (readCount < readTasks.Count)
                    {
                        Task<HttpLockOperation> readTask = await Task.WhenAny<HttpLockOperation>(readTasks);

                        // if any read says locked then bail out
                        if (readTask.Result.HttpLockResponse.StatusCode != HttpStatusCode.OK)
                        {
                            return new HttpResponseMessage(readTask.Result.HttpLockResponse.StatusCode)
                            {
                                Content = new StringContent(JsonSerializer.Serialize(
                                        new LockOperation()
                                        {
                                            LockType = readTask.Result.LockType,
                                            LockId = readTask.Result.LockId
                                        }
                                    )
                                )
                            };
                        }

                        readCount++;
                    }
                }

                List<LockOperation> output = new List<LockOperation>();
                bool isLocked = false;

                while (readCount < readTasks.Count)
                {
                    Task<HttpLockOperation> readTask = await Task.WhenAny<HttpLockOperation>(readTasks);

                    // read all
                    if (readTask.Result.HttpLockResponse.StatusCode != HttpStatusCode.OK)
                    {
                        isLocked = true;

                        output.Add(new LockOperation() { LockType = readTask.Result.LockType, LockId = readTask.Result.LockId });
                    }

                    readCount++;
                }

                if (!isLocked)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }

                return new HttpResponseMessage(HttpStatusCode.Locked)
                {
                    Content = new StringContent(JsonSerializer.Serialize(output))
                };
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(ex.Message)
                };
            }
        }
    }
}