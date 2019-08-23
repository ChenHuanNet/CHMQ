using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SocketHelper
{
    public class MD5Helper
    {
        /// <summary>
        /// MD5加密32位
        /// </summary>
        /// <param name="prestr">需要签名的字符串</param>
        /// <param name="_input_charset">编码格式</param>
        /// <returns>签名结果</returns>
        public static string Sign(string prestr)
        {
            StringBuilder sb = new StringBuilder(32);

            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] t = md5.ComputeHash(Encoding.GetEncoding("utf-8").GetBytes(prestr));
            for (int i = 0; i < t.Length; i++)
            {
                sb.Append(t[i].ToString("x").PadLeft(2, '0'));
            }

            return sb.ToString().ToUpper();
        }
    }
}
