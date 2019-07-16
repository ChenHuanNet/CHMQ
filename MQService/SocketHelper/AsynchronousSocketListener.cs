using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

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

        private uint _msgId = 0;
        /// <summary>
        /// 任务队列
        /// </summary>
        ConcurrentQueue<OperateObject> taskQueue;

        /// <summary>
        /// socket订阅对象
        /// </summary>
        ConcurrentDictionary<string, ConcurrentBag<Socket>> subscribeListSocket = new ConcurrentDictionary<string, ConcurrentBag<Socket>>();

        /// <summary>
        /// http订阅对象
        /// </summary>
        ConcurrentDictionary<string, ConcurrentBag<string>> subscribeListHttp = new ConcurrentDictionary<string, ConcurrentBag<string>>();

        #endregion

        public AsynchronousSocketListener(int port, string ipOrHost = "127.0.0.1", int connectCount = 1000)
        {
            taskQueue = new ConcurrentQueue<OperateObject>();
            this.port = port;
            this.ipOrHost = ipOrHost;
            this.connectCount = connectCount;
        }

        private void CheckJob()
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Enabled = true;
            timer.Interval = 200; //执行间隔时间,单位为毫秒; 这里实际间隔为10分钟  
            timer.Start();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(Job);
        }

        /// <summary>
        /// 启动监听
        /// </summary>
        public void StartListening()
        {
            CheckJob();
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

            Console.WriteLine($"this is conntection {handler.RemoteEndPoint.ToString()}");

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            try
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
                        state.buffer = new byte[StateObject.BufferSize];
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
                        Array.Copy(state.buffer, 0, totalLengthBytes, state.readBufferLength, readLength);
                        state.totalLength = ByteConvert.Bytes2UInt(totalLengthBytes);
                        state.totalBuffer = new byte[state.totalLength];
                    }

                    //还是没读完
                    if (state.totalLength > state.readBufferLength + bytesRead)
                    {
                        Array.Copy(state.buffer, 0, state.totalBuffer, state.readBufferLength, bytesRead);
                        state.readBufferLength += bytesRead;
                        //继续接收
                        state.buffer = new byte[StateObject.BufferSize];
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                        return;
                    }

                    //已经够读完了
                    Array.Copy(state.buffer, 0, state.totalBuffer, state.readBufferLength, state.totalLength - state.readBufferLength);

                    Task.Run(() =>
                    {
                        Handle(state);
                    });



                    //继续接收这个客户端发来的下一个消息
                    StateObject nextState = new StateObject();
                    nextState.workSocket = handler;
                    if (state.readBufferLength + bytesRead > state.totalLength)
                    {
                        //这里说明一个完整的消息体接收完了，有可能还会读到下一次的消息体
                        byte[] newTotalBuffer = new byte[state.readBufferLength + bytesRead - state.totalLength];
                        Array.Copy(state.buffer, state.totalLength - state.readBufferLength, newTotalBuffer, 0, state.readBufferLength + bytesRead - state.totalLength);
                        //要重新建一个对象，不然会影响到异步执行后续处理方法
                        nextState.readBufferLength = newTotalBuffer.Length;
                        if (nextState.readBufferLength >= 4)
                        {
                            //拼出消息体长度 
                            byte[] totalLengthBytes = new byte[4];
                            Array.Copy(newTotalBuffer, 0, totalLengthBytes, 0, 4);
                            nextState.totalLength = ByteConvert.Bytes2UInt(totalLengthBytes);
                            nextState.totalBuffer = new byte[nextState.totalLength];
                        }
                        Array.Copy(newTotalBuffer, 0, nextState.totalBuffer, 0, nextState.readBufferLength);
                    }
                    handler.BeginReceive(nextState.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), nextState);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        private void Send(Socket handler, object data, MsgOperation msgOperation)
        {
            try
            {
                if (_msgId >= uint.MaxValue)
                {
                    _msgId = uint.MinValue;
                }
                _msgId++;
            }
            catch (Exception ex)
            {
                _msgId = uint.MinValue;
            }

            uint msgId = _msgId;
            byte[] sendData = MQPackageHelper.GetPackage(data, msgId, msgOperation);
            // Begin sending the data to the remote device.
            Console.WriteLine($"向[{handler.RemoteEndPoint.ToString()}]发送了一条消息:[{JsonConvert.SerializeObject(data)}]");
            handler.BeginSend(sendData, 0, sendData.Length, 0, new AsyncCallback(SendCallback), handler);
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
            UnPackageObject unPackageObject = MQPackageHelper.UnPackage(state.totalBuffer);

            OperateObject obj = new OperateObject();
            obj.ope = unPackageObject.ope;
            obj.body = unPackageObject.body;
            obj.workSocket = state.workSocket;

            //加入队列
            taskQueue.Enqueue(obj);

            BaseMsgObject baseMsgObject = new BaseMsgObject();
            baseMsgObject.msgId = unPackageObject.msgId;
            baseMsgObject.code = 10000;
            baseMsgObject.message = $"已成功收到[{state.workSocket.RemoteEndPoint.ToString()}][{obj.ope.ToString()}]消息";

            Send(state.workSocket, baseMsgObject, MsgOperation.回复消息);
        }


        private void Job(object source, ElapsedEventArgs e)
        {
            DoTask();
        }

        public void DoTask()
        {
            if (taskQueue.Count > 0)
            {
                bool res = taskQueue.TryDequeue(out OperateObject result);
                if (res)
                {
                    switch (result.ope)
                    {
                        case MsgOperation.发布广播:
                            PublishObject publishObject = (PublishObject)result.body;
                            Task.Run(() =>
                            {
                                //订阅者中有人关注这个话题 就向订阅者发送消息
                                if (subscribeListSocket.ContainsKey(publishObject.topic))
                                {
                                    Console.WriteLine($"准备发布消息给Socket订阅者,订阅数:{subscribeListSocket[publishObject.topic].Count}");
                                    foreach (var socket in subscribeListSocket[publishObject.topic])
                                    {
                                        Send(socket, publishObject.content, MsgOperation.回复消息);
                                    }

                                    subscribeListSocket[publishObject.topic].TryTake(out Socket ss);
                                }

                                if (subscribeListHttp.ContainsKey(publishObject.topic))
                                {
                                    Console.WriteLine($"准备发布消息给Http订阅者,订阅数:{subscribeListHttp[publishObject.topic].Count}");
                                    foreach (string notifyUrl in subscribeListHttp[publishObject.topic])
                                    {
                                        string resp = HttpHelper.PostJsonData(notifyUrl, JsonConvert.SerializeObject(publishObject.content)).Result;
                                    }
                                }
                            });
                            break;
                        case MsgOperation.订阅消息Socket方式:
                            {
                                int failcount = 3;
                                SubscribeObject subscribeObject = (SubscribeObject)result.body;
                            trySubscribeSocketAgain:
                                if (subscribeListSocket.ContainsKey(subscribeObject.topic))
                                {
                                    if (!subscribeListSocket[subscribeObject.topic].Contains(result.workSocket))
                                    {
                                        subscribeListSocket[subscribeObject.topic].Add(result.workSocket);
                                    }
                                }
                                else
                                {
                                    bool success = subscribeListSocket.TryAdd(subscribeObject.topic, new ConcurrentBag<Socket>() { result.workSocket });
                                    if (!success)
                                    {
                                        failcount++;
                                        if (failcount > 3)
                                        {
                                            break;
                                        }
                                        goto trySubscribeSocketAgain;
                                    }
                                }
                            }
                            break;
                        case MsgOperation.订阅消息Http方式:
                            {
                                int failcount = 3;
                                SubscribeObject subscribeObject = (SubscribeObject)result.body;
                                if (string.IsNullOrEmpty(subscribeObject.notifyUrl))
                                {
                                    break;
                                }
                            trySubscribeHttpAgain:
                                if (subscribeListHttp.ContainsKey(subscribeObject.topic))
                                {
                                    if (!subscribeListHttp[subscribeObject.topic].Contains(subscribeObject.notifyUrl))
                                    {
                                        subscribeListHttp[subscribeObject.topic].Add(subscribeObject.notifyUrl);
                                    }
                                }
                                else
                                {
                                    bool success = subscribeListHttp.TryAdd(subscribeObject.topic, new ConcurrentBag<string>() { subscribeObject.notifyUrl });
                                    if (!success)
                                    {
                                        failcount++;
                                        if (failcount > 3)
                                        {
                                            break;
                                        }
                                        goto trySubscribeHttpAgain;
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
