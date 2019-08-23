using SocketHelper;
using System;
using System.Collections.Concurrent;

namespace MQServer
{
    /// <summary>
    /// socket 服务端
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("This is MQ Server");

            AsynchronousSocketListener listener = new AsynchronousSocketListener(10000);
            listener.AddAccessConfig("user1", "password1");
            listener.AddAccessConfig("user2", "password2");
            listener.StartListening();

            Console.ReadLine();
        }


    }
}
