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

            PublishObject publishObject = new PublishObject();
            publishObject.topic = "test";
            publishObject.content = "test1111";

            asynchronousClient.Send(asynchronousClient.client, publishObject, MsgOperation.发布消息);

            Console.ReadLine();
        }
    }
}
