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
    public class AsynchronousSocketListener : IDisposable
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
        ConcurrentDictionary<string, ConcurrentQueue<Socket>> subscribeListSocket = new ConcurrentDictionary<string, ConcurrentQueue<Socket>>();

        /// <summary>
        /// http订阅对象
        /// </summary>
        ConcurrentDictionary<string, ConcurrentQueue<string>> subscribeListHttp = new ConcurrentDictionary<string, ConcurrentQueue<string>>();

        /// <summary>
        /// 发布消息存储
        /// </summary>
        ConcurrentDictionary<string, ConcurrentQueue<object>> publishMsgList = new ConcurrentDictionary<string, ConcurrentQueue<object>>();

        /// <summary>
        /// socket 登录校验结果
        /// </summary>
        ConcurrentDictionary<Socket, bool> loginDic = new ConcurrentDictionary<Socket, bool>();

        /// <summary>
        /// 访问权限设置
        /// </summary>

        ConcurrentDictionary<string, string> accessDic = new ConcurrentDictionary<string, string>();

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
            timer.Interval = 200; //执行间隔时间,单位为毫秒; 
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
            Socket handler = null;
            try
            {

                // Retrieve the state object and the handler socket
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                handler = state.workSocket;
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
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ToString());
                if (ex.ErrorCode == 10054)//断开连接了
                {
                    if (handler == null)
                    {
                        return;
                    }

                    //登录状态移除
                    loginDic.TryRemove(handler, out bool loginResult);

                    Task.Run(() =>
                    {
                        foreach (var item in subscribeListSocket)
                        {
                            //队列移除
                            QueueHelper.Remove(ref subscribeListSocket, item.Key, handler);
                        }
                    });
                }
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

        private void DoTask()
        {
            if (taskQueue.Count > 0)
            {
                bool res = taskQueue.TryDequeue(out OperateObject result);
                if (res)
                {
                    try
                    {
                        if (result.ope != MsgOperation.登录校验)
                        {
                            if (!loginDic.ContainsKey(result.workSocket) || !loginDic[result.workSocket])
                            {
                                Console.WriteLine($" ip=[{result.workSocket.RemoteEndPoint.ToString()}] 尚未登录! 无法执行后续操作");
                                return;
                            }
                        }

                        switch (result.ope)
                        {
                            //这个用服务端主动[推] ,服务端不保存发布消息
                            case MsgOperation.发布广播:
                                {
                                    PublishObject publishObject = (PublishObject)result.body;
                                    Task.Run(() =>
                                    {
                                        //订阅者中有人关注这个话题 就向订阅者发送消息
                                        if (subscribeListSocket.ContainsKey(publishObject.topic))
                                        {
                                            Console.WriteLine($"准备发布消息给Socket订阅者,订阅数:{subscribeListSocket[publishObject.topic].Count}");
                                            //并行执行,任务开销大的时候效率高于单纯的for循环
                                            Parallel.ForEach(subscribeListSocket[publishObject.topic], (socket) =>
                                            {
                                                if (socket != null && socket.Connected)
                                                {
                                                    Send(socket, publishObject.content, MsgOperation.发布广播);
                                                }
                                            });
                                        }

                                        if (subscribeListHttp.ContainsKey(publishObject.topic))
                                        {
                                            Console.WriteLine($"准备发布消息给Http订阅者,订阅数:{subscribeListHttp[publishObject.topic].Count}");
                                            //并行执行,任务开销大的时候效率高于单纯的for循环
                                            Parallel.ForEach(subscribeListHttp[publishObject.topic], (notifyUrl) =>
                                            {
                                                string resp = HttpHelper.PostJsonData(notifyUrl, JsonConvert.SerializeObject(publishObject.content)).Result;
                                            });
                                        }
                                    });
                                }
                                break;
                            //采用得是服务端主动[推]的轮询模式,一个个按顺序发   不推荐使用  客户端主动拉比较好
                            case MsgOperation.发布消息:
                                {
                                    PublishObject publishObject = (PublishObject)result.body;
                                    Task.Run(() =>
                                    {
                                        //订阅者中有人关注这个话题 就向订阅者发送消息
                                        if (subscribeListSocket.ContainsKey(publishObject.topic) && subscribeListSocket[publishObject.topic].Count > 0)
                                        {
                                            //轮询发
                                            Console.WriteLine($"准备发布消息给Socket订阅者,订阅数:{subscribeListSocket[publishObject.topic].Count}");
                                        tryTakeSocketAgain:
                                            subscribeListSocket[publishObject.topic].TryDequeue(out Socket socket);//头部取出来
                                            if (socket == null || !socket.Connected)
                                            {
                                                QueueHelper.Remove(ref subscribeListSocket, publishObject.topic, socket);
                                                goto tryTakeSocketAgain;
                                            }
                                            Send(socket, publishObject.content, MsgOperation.发布消息);
                                            if (!subscribeListSocket[publishObject.topic].Contains(socket))
                                            {
                                                subscribeListSocket[publishObject.topic].Enqueue(socket);//加入到尾部去
                                            }
                                        }

                                        if (subscribeListHttp.ContainsKey(publishObject.topic) && subscribeListHttp[publishObject.topic].Count > 0)
                                        {
                                            Console.WriteLine($"准备发布消息给Http订阅者,订阅数:{subscribeListHttp[publishObject.topic].Count}");
                                        tryTakeHttpAgain:
                                            subscribeListHttp[publishObject.topic].TryDequeue(out string notifyUrl);//头部取出来
                                            string resp = HttpHelper.PostJsonData(notifyUrl, JsonConvert.SerializeObject(publishObject.content)).Result;
                                            if (resp == null || resp.ToUpper() != "SUCCESS")
                                            {
                                                QueueHelper.Remove(ref subscribeListHttp, publishObject.topic, notifyUrl);
                                                goto tryTakeHttpAgain;
                                            }
                                            if (!subscribeListHttp[publishObject.topic].Contains(notifyUrl))
                                            {
                                                subscribeListHttp[publishObject.topic].Enqueue(notifyUrl);//加入到尾部去  
                                            }
                                        }
                                    });
                                }
                                break;
                            //采用客户端来自己[拉]取,服务端保存消息队列,可以实现一个消息只会被一个订阅者拉取消费,这样客户端可以在做一个操作,在自己任务量少于N条时,有余力处理消息就可以向服务器拉取数据,可以保持负载均衡.
                            case MsgOperation.发布消息存消息队列:
                                {
                                    int failcount = 3;
                                    PublishObject publishObject = (PublishObject)result.body;
                                tryStorePublishMsgSocketAgain:
                                    if (publishMsgList.ContainsKey(publishObject.topic))
                                    {
                                        publishMsgList[publishObject.topic].Enqueue(publishObject.content);
                                    }
                                    else
                                    {
                                        ConcurrentQueue<object> _cache = new ConcurrentQueue<object>();
                                        _cache.Enqueue(publishObject.content);
                                        bool success = publishMsgList.TryAdd(publishObject.topic, _cache);
                                        if (!success)
                                        {
                                            failcount++;
                                            if (failcount > 3)
                                            {
                                                break;
                                            }
                                            goto tryStorePublishMsgSocketAgain;
                                        }
                                    }
                                }
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
                                            subscribeListSocket[subscribeObject.topic].Enqueue(result.workSocket);
                                        }
                                    }
                                    else
                                    {
                                        ConcurrentQueue<Socket> _cache = new ConcurrentQueue<Socket>();
                                        _cache.Enqueue(result.workSocket);
                                        bool success = subscribeListSocket.TryAdd(subscribeObject.topic, _cache);
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
                                            subscribeListHttp[subscribeObject.topic].Enqueue(subscribeObject.notifyUrl);
                                        }
                                    }
                                    else
                                    {
                                        ConcurrentQueue<string> _cache = new ConcurrentQueue<string>();
                                        _cache.Enqueue(subscribeObject.notifyUrl);
                                        bool success = subscribeListHttp.TryAdd(subscribeObject.topic, _cache);
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
                            //没有主题或者没消息暂时先不通知客户端
                            case MsgOperation.客户端主动拉取消息:
                                {
                                    SubscribeObject subscribeObject = (SubscribeObject)result.body;
                                    if (publishMsgList.ContainsKey(subscribeObject.topic))
                                    {
                                    tryPullMsgAgain:
                                        if (publishMsgList[subscribeObject.topic].Count > 0)
                                        {
                                            if (string.IsNullOrEmpty(subscribeObject.notifyUrl))
                                            {
                                                //回给socket
                                                bool isGet = publishMsgList[subscribeObject.topic].TryDequeue(out object data);
                                                if (isGet)
                                                {
                                                    Send(result.workSocket, data, MsgOperation.客户端主动拉取消息);
                                                }
                                                else
                                                {
                                                    goto tryPullMsgAgain;
                                                }
                                            }
                                            else
                                            {
                                                //回给notifyUrl
                                                bool isGet = publishMsgList[subscribeObject.topic].TryDequeue(out object data);
                                                if (isGet)
                                                {
                                                    var resp = HttpHelper.PostJsonData(subscribeObject.notifyUrl, JsonConvert.SerializeObject(publishMsgList[subscribeObject.topic])).Result;
                                                }
                                                else
                                                {
                                                    goto tryPullMsgAgain;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case MsgOperation.取消订阅:
                                {
                                    SubscribeObject subscribeObject = (SubscribeObject)result.body;
                                    if (subscribeListSocket.ContainsKey(subscribeObject.topic) && subscribeListSocket[subscribeObject.topic].Contains(result.workSocket))
                                    {
                                        Task.Run(() =>
                                        {
                                            QueueHelper.Remove(ref subscribeListSocket, subscribeObject.topic, result.workSocket);
                                        });
                                    }

                                    if (subscribeListHttp.ContainsKey(subscribeObject.topic) && subscribeListHttp[subscribeObject.topic].Contains(subscribeObject.notifyUrl))
                                    {
                                        Task.Run(() =>
                                        {
                                            QueueHelper.Remove(ref subscribeListHttp, subscribeObject.topic, subscribeObject.notifyUrl);
                                        });
                                    }
                                }
                                break;
                            case MsgOperation.登录校验:
                                {
                                    AccessObject access = (AccessObject)result.body;
                                    AccessResult accessResult = new AccessResult();
                                    if (access != null)
                                    {
                                        if (!accessDic.ContainsKey(access.AccessKeyId))
                                        {
                                            accessResult.Code = -1;
                                            accessResult.Message = $"[{result.workSocket.RemoteEndPoint.ToString()}]您没有登录权限";
                                            Send(result.workSocket, accessResult, MsgOperation.登录校验);
                                            return;
                                        }

                                        DateTime loginTime = Parse.TS2DT(Convert.ToDouble(access.CurrentTimeSpan));
                                        if (loginTime < DateTime.Now.AddMinutes(-5) || loginTime > DateTime.Now.AddMinutes(5))
                                        {
                                            accessResult.Code = -1;
                                            accessResult.Message = $"[{result.workSocket.RemoteEndPoint.ToString()}]CurrentTimeSpan超时";
                                            Send(result.workSocket, accessResult, MsgOperation.登录校验);
                                            return;
                                        }

                                        string prepay = access.AccessKeyId + access.CurrentTimeSpan + accessDic[access.AccessKeyId];
                                        string sign = MD5Helper.Sign(prepay);
                                        if (sign.Equals(access.Sign.ToUpper()))
                                        {
                                            loginDic.TryAdd(result.workSocket, true);
                                            accessResult.Code = 0;
                                            accessResult.Message = $"[{result.workSocket.RemoteEndPoint.ToString()}]登录成功";
                                            Send(result.workSocket, accessResult, MsgOperation.登录校验);
                                        }
                                        else
                                        {
                                            accessResult.Code = -1;
                                            accessResult.Message = $"[{result.workSocket.RemoteEndPoint.ToString()}]登录失败";
                                            Send(result.workSocket, accessResult, MsgOperation.登录校验);
                                        }
                                    }
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"task任务处理失败 ip:[{result.workSocket.RemoteEndPoint.ToString()}] ex:{ex}");
                    }

                }
            }
        }

        /// <summary>
        /// 添加授权用户访问配置信息
        /// </summary>
        /// <param name="accessKeyId"></param>
        /// <param name="accessKeySecret"></param>
        /// <returns></returns>
        public bool AddAccessConfig(string accessKeyId, string accessKeySecret)
        {
            return accessDic.TryAdd(accessKeyId, accessKeySecret);
        }

        #region 构造和析构

        #region IDisposable
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        int disposedFlag;

        /// <summary>
        /// 析构函数 生命结束的时候被调用 和 构造函数相反
        /// </summary>
        ~AsynchronousSocketListener()
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
