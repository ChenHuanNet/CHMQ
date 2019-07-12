using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketHelper
{
    public class AsynchronousClient
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

        public AsynchronousClient(int connectPort, string connectPortIpOrHost = "127.0.0.1")
        {
            this.port = connectPort;
            this.ipOrHost = connectPortIpOrHost;
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
                // Send test data to the remote device.
                // Receive the response from the remote device.
                Receive(client);
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
                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    // Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.
                    receiveDone.Set();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public void Send(Socket client, object data, MsgOperation msgOperation)
        {
            byte[] byteData = ByteConvert.ObjToByte(data);

            byte[] sendData = new byte[12 + byteData.Length];
            uint totalLength = (uint)(12 + byteData.Length);
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
            Array.Copy(ByteConvert.UInt2Bytes(totalLength), 0, sendData, 0, 4);
            Array.Copy(ByteConvert.UInt2Bytes(msgId), 0, sendData, 4, 4);
            Array.Copy(ByteConvert.UInt2Bytes((uint)msgOperation), 0, sendData, 8, 4);
            Array.Copy(byteData, 0, sendData, 12, byteData.Length);

            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);

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

    }
}
