﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace SocketHelper
{
    [Serializable]
    /// <summary>
    /// 订阅消息体
    /// </summary>
    public class SubscribeObject
    {
        /// <summary>
        /// 订阅的消息标题
        /// </summary>
        public string topic { get; set; }

        /// <summary>
        /// Http方式下使用
        /// </summary>
        public string notifyUrl { get; set; }

    }
}
