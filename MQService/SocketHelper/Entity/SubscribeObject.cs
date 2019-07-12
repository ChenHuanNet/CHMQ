using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace SocketHelper.Entity
{
    /// <summary>
    /// 订阅消息体
    /// </summary>
    public class SubscribeObject
    {
        /// <summary>
        /// 当前连接的socket对象
        /// </summary>
        public Socket wordSocket { get; set; }

        
    }
}
