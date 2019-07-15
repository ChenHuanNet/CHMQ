using System.Net.Sockets;
using System.Text;

namespace SocketHelper
{
    public class StateObject
    {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];

        /// <summary>
        /// 一次完整的请求数据
        /// </summary>
        public byte[] totalBuffer = new byte[BufferSize];

        /// <summary>
        /// 已经读取的长度大小
        /// </summary>
        public int readBufferLength { get; set; }


        #region 消息头 占12个字节
        /// <summary>
        /// uint 占4个字节 消息体总长度包含头部
        /// </summary>
        public uint totalLength { get; set; }

        /// <summary>
        /// uint 占4个字节 什么类型的话题操作
        /// </summary>
        public MsgOperation ope { get; set; }

        /// <summary>
        /// uint 占4个字节 消息编号 一定时间内唯一
        /// </summary>
        public uint msgId { get; set; }

        #endregion
    }
}