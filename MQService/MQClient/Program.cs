using Newtonsoft.Json;
using SocketHelper;
using System;
using System.Text;
using System.Threading;

namespace MQClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"This is a Client");

            AsynchronousClient asynchronousClient = new AsynchronousClient(10000);
            asynchronousClient.msgReceiveEvent += Handle;
            asynchronousClient.StartClient();

            //Subscribe(asynchronousClient);

            //Thread.Sleep(200);

            Publish(asynchronousClient);

            //Publish(asynchronousClient);

            //Publish(asynchronousClient);

            while (true)
            {
                var input = Console.ReadLine();
                if (input == "1")
                {
                    Subscribe(asynchronousClient);
                }
                else if (input == "2")
                {
                    Publish(asynchronousClient);
                }
                else if (input == "exit")
                {
                    break;
                }
            }

            Console.ReadLine();
        }

        private static void Subscribe(AsynchronousClient asynchronousClient)
        {
            SubscribeObject subscribeObject = new SubscribeObject();
            subscribeObject.topic = "test";
            Console.WriteLine($"我订阅了主题[{subscribeObject.topic}]");
            asynchronousClient.Send(subscribeObject, MsgOperation.订阅消息);

        }

        private static void Publish(AsynchronousClient asynchronousClient)
        {
            PublishObject publishObject = new PublishObject();
            publishObject.topic = "test";
            publishObject.content = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + new Random().Next(999).ToString();
            Console.WriteLine($"我在主题[{publishObject.topic}]发布了一条消息:{publishObject.content}");
            asynchronousClient.Send(publishObject, MsgOperation.发布消息);
        }

        /// <summary>
        /// 接完到完整消息后处理消息
        /// </summary>
        /// <param name="state"></param>
        private static void Handle(UnPackageObject obj)
        {
            string result = JsonConvert.SerializeObject(obj.body);
            Console.WriteLine(obj.ope.ToString() + ":" + result);
        }

    }
}
