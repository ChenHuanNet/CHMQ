using System;
using System.Collections.Generic;
using System.Text;

namespace SocketHelper
{
    public class BaseMsgObject
    {
        public uint msgId { get; set; }

        public int code { get; set; }

        public string message { get; set; }
    }
}
