using System;
using System.Collections.Generic;
using System.Text;

namespace SocketHelper
{
    [Serializable]
    public class AccessObject
    {
        public string AccessKeyId { get; set; }

        public string CurrentTimeSpan { get; set; }

        public string Sign { get; set; }
    }
}
