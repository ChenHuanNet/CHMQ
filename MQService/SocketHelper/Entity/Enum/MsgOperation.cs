using System;
using System.Collections.Generic;
using System.Text;

namespace SocketHelper
{
    public enum MsgOperation : uint
    {
        未知 = 0,
        发布广播 = 1,//会被所有订阅消费
        发布消息,//只有一个订阅者消费
        发布消息存消息队列,
        订阅消息Socket方式,
        订阅消息Http方式,
        客户端主动拉取消息,//对应的[发布消息存消息队列]
        取消订阅,
        回复消息
    }
}
