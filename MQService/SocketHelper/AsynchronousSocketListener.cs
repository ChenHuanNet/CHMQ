using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketHelper
{
    // State object for reading client data asynchronously
    public class AsynchronousSocketListener
    {
        #region 参数
        public int port;
        public string ipOrHost;
        public int connectCount;
        // Thread signal.

        public ManualResetEvent allDone = new ManualResetEvent(false);

        /// <summary>
        /// 任务队列
        /// </summary>
        ConcurrentQueue<BaseMsgObject> taskQueue;

        /// <summary>
        /// 订阅对象
        /// </summary>
        ConcurrentDictionary<string, ConcurrentBag<Socket>> subscribeList = new ConcurrentDictionary<string, ConcurrentBag<Socket>>();

        #endregion

        public AsynchronousSocketListener(int port, string ipOrHost = "127.0.0.1", int connectCount = 1000)
        {
            taskQueue = new ConcurrentQueue<BaseMsgObject>();
            this.port = port;
            this.ipOrHost = ipOrHost;
            this.connectCount = connectCount;
        }

        /// <summary>
        /// 启动监听
        /// </summary>
        public void StartListening()
        {
            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];
            // Establish the local endpoint for the socket.
            // The DNS name of the computer
            // running the listener is "host.contoso.com".
            //IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = Dns.GetHostAddresses(ipOrHost)[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(connectCount);
                while (true)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();
                    // Start an asynchronous socket to listen for connections.
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");

            Console.Read();

        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();
            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            //接收到远程客户端的连接，新建一个socket对象去处理该连接发起的请求
            Socket handler = listener.EndAccept(ar);
            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);
            if (bytesRead > 0)
            {
                //不管是不是第一次接收，只要接收字节总长度小于4
                if (state.readBufferLength + bytesRead < 4)
                {
                    Array.Copy(state.buffer, 0, state.totalBuffer, state.readBufferLength, bytesRead);
                    state.readBufferLength += bytesRead;
                    //继续接收
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                    return;
                }

                //已读长度如果小于4 要先获取总的消息长度
                if (state.readBufferLength < 4)
                {
                    //先拼出消息体长度 
                    byte[] totalLengthBytes = new byte[4];
                    if (state.readBufferLength > 0)
                    {
                        Array.Copy(state.totalBuffer, 0, totalLengthBytes, 0, state.readBufferLength);
                    }
                    int readLength = 4 - state.readBufferLength;
                    Array.Copy(state.buffer, 0, totalLengthBytes, totalLengthBytes.Length, readLength);
                    state.totalLength = ByteConvert.Byte2UintEasy(totalLengthBytes);
                }

                //还是没读完
                if (state.totalLength > state.readBufferLength + bytesRead)
                {
                    Array.Copy(state.buffer, state.readBufferLength, state.totalBuffer, state.readBufferLength, bytesRead);
                    state.readBufferLength += bytesRead;
                    //继续接收
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                    return;
                }

                //已经够读完了
                Array.Copy(state.buffer, state.readBufferLength, state.totalBuffer, state.readBufferLength, state.totalLength - state.readBufferLength);

                Task.Run(() =>
                {
                    Handle(state);
                });

                if (state.readBufferLength + bytesRead > state.totalLength)
                {
                    //这里说明一个完整的消息体接收完了，有可能还会读到下一次的消息体
                    byte[] newTotalBuffer = new byte[state.readBufferLength + bytesRead - state.totalLength];
                    Array.Copy(state.totalBuffer, state.totalLength, newTotalBuffer, 0, state.readBufferLength + bytesRead - state.totalLength);
                    state = new StateObject();
                    state.workSocket = handler;
                    state.totalBuffer = newTotalBuffer;
                    state.readBufferLength = newTotalBuffer.Length;
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
            }

        }

        private void Send(Socket handler, object data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = ByteConvert.ObjToByte(data);
            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;
                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// 接完到完整消息后处理消息
        /// </summary>
        /// <param name="state"></param>
        private void Handle(StateObject state)
        {
            //读取话题
            byte[] topicBytes = new byte[4];
            Array.Copy(state.totalBuffer, 4, topicBytes, 0, 4);
            state.ope = (MsgOperation)ByteConvert.Byte2UintEasy(topicBytes);
            //读取消息ID
            byte[] msgIdBytes = new byte[4];
            Array.Copy(state.totalBuffer, 8, msgIdBytes, 0, 4);
            state.ope = (MsgOperation)ByteConvert.Byte2UintEasy(msgIdBytes);
            //读取消息体
            byte[] bodyBytes = new byte[state.totalLength - 12];
            Array.Copy(state.totalBuffer, 12, bodyBytes, 0, state.totalLength - 12);

            BaseMsgObject obj = new BaseMsgObject();
            obj.ope = state.ope;
            obj.body = ByteConvert.ByteToObj(bodyBytes, bodyBytes.Length);
            obj.workSocket = state.workSocket;
            //obj.sendFuc = Send;
            //obj.sendFucCallBack = SendCallback;

            //加入队列
            taskQueue.Enqueue(obj);
        }
    }
}
