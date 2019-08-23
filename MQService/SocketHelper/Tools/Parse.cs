using System;
using System.Collections.Generic;
using System.Text;

namespace SocketHelper
{
    public class Parse
    {
        /// <summary>
        /// 时间转时间戳
        /// </summary>
        /// <returns></returns>
        public static string DT2TS(DateTime dt)
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return ((long)ts.TotalSeconds).ToString();
        }

        /// <summary>        
        /// 时间戳转为C#格式时间秒    
        /// </summary>        
        /// <param name=”timeStamp”></param>        
        /// <returns></returns>        
        public static DateTime TS2DT(double timeStamp)
        {
            var start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return start.AddSeconds(timeStamp).AddHours(8);
        }
    }
}
