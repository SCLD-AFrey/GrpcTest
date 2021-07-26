using System;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using GrpcBroadcast.Common;
using GrpcBroadcast.Client.Core;

namespace Test.CLI
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var originId = Guid.NewGuid().ToString();

            System.Console.WriteLine($"Joined as {originId}");
            var broadcastServiceClient = new BroadcastServiceClient();
            var consoleLock = new object();

            // subscribe (asynchronous)
            _ = broadcastServiceClient.BroadcastLogs()
                .ForEachAsync(x =>
                {
                    // if the user is writing something, wait until it finishes.
                    lock (consoleLock)
                    {
                        System.Console.WriteLine($"{x.At.ToDateTime().ToString("HH:mm:ss")} {x.OriginId}: {x.Content}");
                    }
                });

            // write
            while (true)
            {
                var key = System.Console.ReadKey();

                // A key input starts writing mode
                lock (consoleLock)
                {
                    var content = key.KeyChar + System.Console.ReadLine();

                  

                    broadcastServiceClient.Write(new BroadcastLog()
                    {
                        OriginId = originId,
                        Content = content,
                        At = Timestamp.FromDateTime(DateTime.Now.ToUniversalTime())
                    }).Wait();
                }
            }
        }
    }
}