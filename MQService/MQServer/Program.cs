using SocketHelper;
using System;
using System.Collections.Concurrent;

namespace MQServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("This is MQ Server");

            AsynchronousSocketListener listener = new AsynchronousSocketListener(10000);
            listener.StartListening();

            Console.ReadLine();
        }


    }
}
