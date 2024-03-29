﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SocketHelper
{
    [Serializable]
    /// <summary>
    /// 发布消息体
    /// </summary>
    public class PublishObject
    {
        /// <summary>
        /// 订阅的消息标题
        /// </summary>
        public string topic { get; set; }

        /// <summary>
        /// 消息内容 对象
        /// </summary>
        public object content { get; set; }
    }
}
