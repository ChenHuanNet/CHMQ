using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketHelper
{
    public class SocketHttpListener : IDisposable
    {
        Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);  //侦听socket
        AsynchronousClient asynchronousClient;
        public SocketHttpListener(int port)
        {
            _socket.Bind(new IPEndPoint(IPAddress.Any, port));
            _socket.Listen(10000);

            //连接转发的socket服务端
            asynchronousClient = new AsynchronousClient(10000);
            asynchronousClient.msgReceiveEvent += Handle;
        }

        public void Start()
        {
            _socket.BeginAccept(new AsyncCallback(OnAccept), _socket);  //开始接收来自浏览器的http请求（其实是socket连接请求）
            Console.WriteLine("Socket Web Server 已启动监听！" + Environment.NewLine + "  监听端口：" + ((IPEndPoint)_socket.LocalEndPoint).Port);

            asynchronousClient.StartClient();
        }

        /// <summary>
        /// 接受处理http的请求
        /// </summary>
        /// <param name="ar"></param>
        void OnAccept(IAsyncResult ar)
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
                Console.WriteLine("接收到Http请求数据: " + recv_request);  //将请求显示到界面

                //使用JSON
                object json = LoadJsonFormBody(recv_request, out MsgOperation ope, out string errmsg);

                SocketHttpResponseModel response = new SocketHttpResponseModel();
                if (!string.IsNullOrEmpty(errmsg))
                {
                    response.code = -1;
                    response.message = errmsg;
                }
                else
                {
                    response.code = 10000;
                    response.message = "请求成功";

                    //转发给socket
                    Task.Run(() =>
                    {
                        if (asynchronousClient != null)
                        {
                            asynchronousClient.Send(json, ope);
                        }
                    });
                }
                //byte[] cont = pageHandle(RouteHandle(recv_request));
                byte[] cont = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
                sendPageContent(cont, web_client);


                //NO.2  串行处理http请求
            }
            catch (Exception ex)
            {
                Console.WriteLine("处理http请求时出现异常！" + Environment.NewLine + "\t" + ex.Message);
            }
        }
        /// <summary>
        /// 数据返回
        /// </summary>
        /// <param name="pageContent"></param>
        /// <param name="response"></param>
        void sendPageContent(byte[] pageContent, Socket response)
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
        /// 从Http 中获取JSON   请求方式是POST FromBody
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="errmsg"></param>
        /// <returns></returns>
        object LoadJsonFormBody(string request, out MsgOperation msgOperation, out string errmsg)
        {
            msgOperation = MsgOperation.未知;
            string[] strs = request.Split(new string[] { "\r\n" }, StringSplitOptions.None);  //以“换行”作为切分标志
            StringBuilder sb = new StringBuilder();
            if (strs.Length > 0)
            {
                string[] line1 = strs[0].Split(' ');
                try
                {
                    string ope = line1[1].Split('?')[1].Split('=')[1];

                    if (!Enum.TryParse(ope, out msgOperation))
                    {
                        errmsg = "ope操作类型不存在";
                    }
                }
                catch (Exception ex)
                {
                    errmsg = "请在url地址上传入操作类型ope";
                    return null;
                }

                bool isStart = false;
                foreach (var item in strs)
                {
                    if (item.Trim() == string.Empty && !isStart)
                    {
                        isStart = true;
                        continue;
                    }
                    if (isStart)
                    {
                        sb.Append(item);
                    }
                }
            }

            if (sb.ToString().Trim() == string.Empty)
            {
                errmsg = "请传入请求的JSON";
            }

            if (msgOperation == MsgOperation.未知)
            {
                errmsg = "请传入正确的操作类型";
            }


            object t = GetObjectT(sb.ToString(), msgOperation, out errmsg);

            return t;
        }


        /// <summary>
        /// 获取POST请求数据  这个是普通的 key=value的形式  x-www-form-urlencoded
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Dictionary<string, string> LoadPostParamters(string request, out string errmsg)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            errmsg = "";
            string[] strs = request.Split(new string[] { "\r\n" }, StringSplitOptions.None);  //以“换行”作为切分标志
            if (strs.Length > 0)  //解析出请求路径、post传递的参数(get方式传递参数直接从url中解析)
            {
                //items[1]表示请求url中的路径部分（不含主机部分）
                string[] items = strs[0].Split(' ');
                // string pageName = items[1];
                string requestType = items[0];

                if (requestType.ToUpper() != "POST")
                {
                    errmsg = "post参数不能为空";
                    return new Dictionary<string, string>();
                }
                string post_data = strs[strs.Length - 1]; //最后一项
                dict = ParameterHandle(strs);
            }

            if (dict.Count <= 0)
            {
                errmsg = "post参数不能为空";
            }
            return dict;

        }


        /// <summary>
        /// 按照HTTP协议格式,解析浏览器发送的请求字符串  获取POST请求数据
        /// </summary>
        /// <param name="strs"></param>
        /// <returns></returns>
        Dictionary<string, string> ParameterHandle(string[] strs)
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


        object GetObjectT(string json, MsgOperation msgOperation, out string errmsg)
        {
            object t = null;
            try
            {
                errmsg = "";
                switch (msgOperation)
                {
                    case MsgOperation.发布广播:
                        t = JsonConvert.DeserializeObject<PublishObject>(json);
                        break;
                    case MsgOperation.订阅消息Http方式:
                        SubscribeObject subscribe = JsonConvert.DeserializeObject<SubscribeObject>(json);
                        if (string.IsNullOrEmpty(subscribe.notifyUrl))
                        {
                            errmsg = "订阅消息Http方式,请传入异步通知地址notifyUrl";
                        }
                        t = subscribe;
                        break;
                }
            }
            catch (Exception ex)
            {
                errmsg = $"JSON解析失败,json:[{json}]";
            }


            return t;
        }

        void Handle(UnPackageObject obj)
        {
            string result = JsonConvert.SerializeObject(obj.body);
            Console.WriteLine(obj.ope.ToString() + ":" + result);
        }


        #region 构造和析构

        #region IDisposable
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        int disposedFlag;

        /// <summary>
        /// 析构函数 生命结束的时候被调用 和 构造函数相反
        /// </summary>
        ~SocketHttpListener()
        {
            Dispose(false);
        }

        /// <summary>
        /// 释放所占用的资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 获取该对象是否已经被释放
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        public bool IsDisposed
        {
            get
            {
                return disposedFlag != 0;
            }
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (System.Threading.Interlocked.Increment(ref disposedFlag) != 1) return;
            if (disposing)
            {
                //在这里编写托管资源释放代码
            }
            //在这里编写非托管资源释放代码
        }

        #endregion
    }
}
