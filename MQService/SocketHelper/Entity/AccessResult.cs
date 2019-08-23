using System;
using System.Collections.Generic;
using System.Text;

namespace SocketHelper
{
    [Serializable]
    public class AccessResult
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }
}
