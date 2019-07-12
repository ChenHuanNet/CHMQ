using System;
using System.Collections.Generic;
using System.Text;

namespace SocketHelper
{
    public enum MsgOperation : uint
    {
        发布消息 = 1,
        订阅消息,
    }
}
