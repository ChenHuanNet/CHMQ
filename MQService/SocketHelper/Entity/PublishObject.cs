using System;
using System.Collections.Generic;
using System.Text;

namespace SocketHelper
{
    /// <summary>
    /// 发布消息实体
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
