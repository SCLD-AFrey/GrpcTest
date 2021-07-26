using System;
using GrpcBroadcast.Server.Core;
using GrpcBroadcast.Server.Core.Infrastructure;

namespace Test.Server
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Broadcast service is starting.");

            MefManager.Initialize();

            foreach (var service in MefManager.Container.GetExportedValues<IService>()) service.Start();

            MefManager.Container.GetExportedValue<Logger>().GetLogsAsObservable().Subscribe(x => Console.WriteLine(x));

            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
            }
        }
    }
}