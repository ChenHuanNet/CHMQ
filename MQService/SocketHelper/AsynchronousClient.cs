using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketHelper
{
    public class AsynchronousClient : IDisposable
    {

        // The port number for the remote device.
        private uint _msgId = 0;
        private int port = 11000;
        private string ipOrHost;

        // ManualResetEvent instances signal completion.

        private ManualResetEvent connectDone = new ManualResetEvent(false);

        private ManualResetEvent sendDone = new ManualResetEvent(false);

        private ManualResetEvent receiveDone = new ManualResetEvent(false);

        // The response from the remote device.

        private static string response = string.Empty;

        public Socket client;

        public MsgReceiveEvent msgReceiveEvent;

        public delegate void MsgReceiveEvent(UnPackageObject state);

        public AsynchronousClient(int connectPort, string connectPortIpOrHost = "127.0.0.1", MsgReceiveEvent msgReceiveEvent = null)
        {
            this.port = connectPort;
            this.ipOrHost = connectPortIpOrHost;
            this.msgReceiveEvent = msgReceiveEvent;
        }

        public void StartClient()
        {
            // Connect to a remote device.
            try
            {
                // Establish the remote endpoint for the socket.
                // The name of the remote device is "host.contoso.com".
                IPAddress ipAddress = Dns.GetHostAddresses(ipOrHost)[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);
                // Create a TCP/IP socket.
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // Connect to the remote endpoint.
                client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

                // Receive the response from the remote device.

                Task.Run(() =>
                {
                    Receive(client);
                });


                // Write the response to the console.
                Console.WriteLine("Response received : {0}", response);
                // Release the socket.
                //client.Shutdown(SocketShutdown.Both);
                //client.Close();
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;
                // Complete the connection.
                client.EndConnect(ar);
                Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());
                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;
                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                receiveDone.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;
                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);
                Console.WriteLine($"[{client.LocalEndPoint.ToString()}]接收到[{bytesRead}]字节数据");
                if (bytesRead > 0)
                {
                    //不管是不是第一次接收，只要接收字节总长度小于4
                    if (state.readBufferLength + bytesRead < 4)
                    {
                        Array.Copy(state.buffer, 0, state.totalBuffer, state.readBufferLength, bytesRead);
                        state.readBufferLength += bytesRead;
                        //继续接收
                        state.buffer = new byte[StateObject.BufferSize];
                        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
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
                        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                        return;
                    }

                    //已经够读完了
                    Array.Copy(state.buffer, 0, state.totalBuffer, state.readBufferLength, state.totalLength - state.readBufferLength);

                    Console.WriteLine($"[{client.LocalEndPoint.ToString()}]接收包总长度[{state.totalLength}]字节数据");

                    UnPackageObject unPackageObject = MQPackageHelper.UnPackage(state.totalBuffer);

                    //接收成功事件不为null时触发
                    if (msgReceiveEvent != null)
                    {
                        Task.Run(() =>
                        {
                            msgReceiveEvent(unPackageObject);
                        });
                    }

                    //接收完完整的一个包，休息200毫秒  间隔太短可能导致实体类的值被覆盖
                    Thread.Sleep(200);

                    //继续接收这个客户端发来的下一个消息
                    StateObject nextState = new StateObject();
                    nextState.workSocket = client;
                    //当超出当前接收内容时触发
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

                    client.BeginReceive(nextState.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), nextState);

                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public void Send(object data, MsgOperation msgOperation)
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

            client.BeginSend(sendData, 0, sendData.Length, 0, new AsyncCallback(SendCallback), client);

            sendDone.WaitOne();
        }



        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;
                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);
                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        #region 构造和析构

        #region IDisposable
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        int disposedFlag;

        /// <summary>
        /// 析构函数 生命结束的时候被调用 和 构造函数相反
        /// </summary>
        ~AsynchronousClient()
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
                client.Close();
                client.Dispose();
            }
            //在这里编写非托管资源释放代码
        }

        #endregion

    }
}
