using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using SocketHelper;
using System.Collections.Concurrent;

namespace HttpWebService
{
    /// <summary>
    /// 监听http请求的服务端  请求体使用json body的形式
    /// </summary>
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine($"This is a HttpServer");

            SocketHttpListener socketHttpListener = new SocketHttpListener(10001);
            socketHttpListener.Start();
            Console.ReadKey();
        }
    }
}