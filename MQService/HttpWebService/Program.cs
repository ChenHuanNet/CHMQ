using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace HttpWebService
{
    class Program
    {
        static Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);  //侦听socket

        static void Main(string[] args)
        {
            _socket.Bind(new IPEndPoint(IPAddress.Any, 8080));
            _socket.Listen(100);
            _socket.BeginAccept(new AsyncCallback(OnAccept), _socket);  //开始接收来自浏览器的http请求（其实是socket连接请求）
            writeLog("Socket Web Server 已启动监听！" + Environment.NewLine + "  监听端口：" + ((IPEndPoint)_socket.LocalEndPoint).Port);
            Console.ReadKey();
        }

        /// <summary>
        /// 接受处理http的请求
        /// </summary>
        /// <param name="ar"></param>
        static void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket socket = ar.AsyncState as Socket;
                Socket web_client = socket.EndAccept(ar);  //接收到来自浏览器的代理socket
                //NO.1  并行处理http请求
                socket.BeginAccept(new AsyncCallback(OnAccept), socket); //开始下一次http请求接收   （此行代码放在NO.2处时，就是串行处理http请求，前一次处理过程会阻塞下一次请求处理）

                byte[] recv_Buffer = new byte[1024 * 640];
                int recv_Count = web_client.Receive(recv_Buffer);  //接收浏览器的请求数据
                string recv_request = Encoding.UTF8.GetString(recv_Buffer, 0, recv_Count);
                writeLog("接收到Http请求数据: " + recv_request);  //将请求显示到界面
                string url = RouteHandle(recv_request);
                //byte[] cont = pageHandle(RouteHandle(recv_request));
                byte[] cont = Encoding.UTF8.GetBytes($"{{\"code\":10000,\"message\":\"服务器已成功接收数据消息\"}}");
                sendPageContent(cont, web_client);


                //NO.2  串行处理http请求
            }
            catch (Exception ex)
            {
                writeLog("处理http请求时出现异常！" + Environment.NewLine + "\t" + ex.Message);
            }
        }
        /// <summary>
        /// 数据返回
        /// </summary>
        /// <param name="pageContent"></param>
        /// <param name="response"></param>
        static void sendPageContent(byte[] pageContent, Socket response)
        {

            string statusline = "HTTP/1.1 200 OK\r\n";   //状态行
            byte[] statusline_to_bytes = Encoding.UTF8.GetBytes(statusline);

            byte[] content_to_bytes = pageContent;

            string header = string.Format("Content-Type:application/json;charset=UTF-8\r\nContent-Length:{0}\r\n", content_to_bytes.Length);
            byte[] header_to_bytes = Encoding.UTF8.GetBytes(header);  //应答头


            response.Send(statusline_to_bytes);  //发送状态行
            response.Send(header_to_bytes);  //发送应答头
            response.Send(new byte[] { (byte)'\r', (byte)'\n' });  //发送空行
            response.Send(content_to_bytes);  //发送正文（html）

            response.Close();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        static string RouteHandle(string request)
        {
            string retRoute = "";
            string[] strs = request.Split(new string[] { "\r\n" }, StringSplitOptions.None);  //以“换行”作为切分标志
            if (strs.Length > 0)  //解析出请求路径、post传递的参数(get方式传递参数直接从url中解析)
            {
                string[] items = strs[0].Split(' ');  //items[1]表示请求url中的路径部分（不含主机部分）
                string pageName = items[1];
                string post_data = strs[strs.Length - 1]; //最后一项
                Dictionary<string, string> dict = ParameterHandle(strs);

                retRoute = pageName + (post_data.Length > 0 ? "?" + post_data : "");
            }

            return retRoute;

        }


        /// <summary>
        /// 按照HTTP协议格式,解析浏览器发送的请求字符串
        /// </summary>
        /// <param name="strs"></param>
        /// <returns></returns>
        static Dictionary<string, string> ParameterHandle(string[] strs)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();

            if (strs.Length > 0)  //解析出请求路径、post传递的参数(get方式传递参数直接从url中解析)
            {
                if (strs.Contains(""))  //包含空行  说明存在post数据
                {
                    string post_data = strs[strs.Length - 1]; //最后一项
                    if (post_data != "")
                    {
                        string[] post_datas = post_data.Split('&');
                        foreach (string s in post_datas)
                        {
                            param.Add(s.Split('=')[0], s.Split('=')[1]);
                        }
                    }
                }
            }
            return param;
        }



        static void writeLog(string msg)
        {
            Console.WriteLine("  " + msg);
        }




    }
}