using SocketHelper;
using System;

namespace MQClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"This is a Client");

            AsynchronousClient asynchronousClient = new AsynchronousClient(10000);
            asynchronousClient.StartClient();



            Console.ReadLine();
        }
    }
}
