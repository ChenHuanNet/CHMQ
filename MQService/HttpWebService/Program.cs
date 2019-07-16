using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using SocketHelper;

namespace HttpWebService
{
    class Program
    {

        static void Main(string[] args)
        {
            SocketHttpListener socketHttpListener = new SocketHttpListener(10001);
            socketHttpListener.Start();
            Console.ReadKey();
        }
    }
}