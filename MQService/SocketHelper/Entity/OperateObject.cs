using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace SocketHelper
{
    public class OperateObject
    {
        public MsgOperation ope { get; set; }

        public object body { get; set; }

        public Socket workSocket { get; set; }

        //public SendFuc sendFuc { get; set; }

        //public SendFucCallBack sendFucCallBack { get; set; }
    }

    //public delegate void SendFuc(Socket handler, object data);

    //public delegate void SendFucCallBack(IAsyncResult ar);
}
