using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace SocketHelper
{
    public class ByteConvert
    {
        #region 二进制和uint 转换
        public static uint Byte2UintEasy(byte[] bytes)
        {
            //将数据倒序一下
            Array.Reverse(bytes);
            uint Ret = BitConverter.ToUInt32(bytes, 0);
            return Ret;
        }

        /// <summary>
        /// byte[4] 转 uint
        /// </summary>
        /// <param name="bs"></param>
        /// <returns></returns>
        public static uint Bytes2UInt(byte[] bs)  //没有指定起始索引
        {
            return (Bytes2UInt(bs, 0));
        }

        /// <summary>
        /// byte[4] 转 uint
        /// </summary>
        /// <param name="bs"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static uint Bytes2UInt(byte[] bs, int startIndex) //返回字节数组代表的整数数字，4个数组
        {
            byte[] t = new byte[4];
            for (int i = 0; i < 4 && i < bs.Length - startIndex; i++)
            {
                t[i] = bs[startIndex + i];
            }
            //这个其实也是 倒序 (0 和 3 换  1 和 2 换)    ,原本 0123  变成  3210
            byte b = t[0];
            t[0] = t[3];
            t[3] = b;
            b = t[1];
            t[1] = t[2];
            t[2] = b;
            return (BitConverter.ToUInt32(t, 0));
        }

        /// <summary>
        /// uint 转 byte[4]
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static byte[] UInt2Bytes(uint i)
        {
            byte[] t = BitConverter.GetBytes(i);
            byte b = t[0];
            t[0] = t[3];
            t[3] = b;
            b = t[1];
            t[1] = t[2];
            t[2] = b;
            return (t);
        }
        #endregion

        #region 二进制对象互转
        /// <summary>
        /// 二进制转对象
        /// </summary>
        /// <param name="b">byte</param>
        /// <param name="length">byte实际读取长度</param>
        /// <returns></returns>
        public static object ByteToObj(byte[] b, int length)
        {
            MemoryStream ms = new MemoryStream(b, 0, length);
            BinaryFormatter iFormatter = new BinaryFormatter();
            object obj = iFormatter.Deserialize(ms);

            return obj;
        }

        /// <summary>
        /// 将对象转换为byte数组
        /// </summary>
        /// <param name="obj">被转换对象</param>
        /// <returns>转换后byte数组</returns>
        public static byte[] ObjToByte(object obj)
        {
            MemoryStream ms = new MemoryStream();

            BinaryFormatter iFormatter = new BinaryFormatter();
            iFormatter.Serialize(ms, obj);//使用这个要把对象设置成可序列化对象[Serializable]
            byte[] buff = ms.GetBuffer();

            return buff;
        }
        #endregion
    }
}
