using System;
using System.Collections.Generic;
using System.Text;

namespace SocketHelper
{
    /// <summary>
    /// 解析包
    /// </summary>
    public class UnPackageObject
    {
        public uint totalLength { get; set; }

        /// <summary>
        /// uint 占4个字节 什么类型的话题操作
        /// </summary>
        public MsgOperation ope { get; set; }

        /// <summary>
        /// uint 占4个字节 消息编号 一定时间内唯一
        /// </summary>
        public uint msgId { get; set; }

        /// <summary>
        /// 消息体内容
        /// </summary>
        public object body;

    }
}
