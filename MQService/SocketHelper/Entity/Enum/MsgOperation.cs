using System;
using System.Collections.Generic;
using System.Text;

namespace SocketHelper
{
    public enum MsgOperation : uint
    {
        未知 = 0,
        发布消息 = 1,
        订阅消息,
        回复消息
    }
}
