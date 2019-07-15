using System;
using System.Collections.Generic;
using System.Text;

namespace SocketHelper
{
    /// <summary>
    /// 组装和解析消息包
    /// </summary>
    public class MQPackageHelper
    {
        /// <summary>
        /// 组装将要发送的数据包 含包头
        /// </summary>
        /// <param name="data">消息体</param>
        /// <param name="msgId">消息ID</param>
        /// <param name="msgOperation">操作类型</param>
        /// <returns></returns>
        public static byte[] GetPackage(object data, uint msgId, MsgOperation msgOperation)
        {
            byte[] byteData = ByteConvert.ObjToByte(data);

            byte[] sendData = new byte[12 + byteData.Length];
            uint totalLength = (uint)(12 + byteData.Length);

            Array.Copy(ByteConvert.UInt2Bytes(totalLength), 0, sendData, 0, 4);
            Array.Copy(ByteConvert.UInt2Bytes((uint)msgOperation), 0, sendData, 4, 4);
            Array.Copy(ByteConvert.UInt2Bytes(msgId), 0, sendData, 8, 4);

            Array.Copy(byteData, 0, sendData, 12, byteData.Length);

            return sendData;
        }


        public static UnPackageObject UnPackage(byte[] totalBuffer)
        {
            UnPackageObject unPackageObject = new UnPackageObject();
            //读取消息总长
            byte[] totalLengthBytes = new byte[4];
            Array.Copy(totalBuffer, 0, totalLengthBytes, 0, 4);
            uint totalLength = ByteConvert.Bytes2UInt(totalLengthBytes);
            unPackageObject.totalLength = totalLength;
            //读取话题
            byte[] topicBytes = new byte[4];
            Array.Copy(totalBuffer, 4, topicBytes, 0, 4);
            unPackageObject.ope = (MsgOperation)ByteConvert.Bytes2UInt(topicBytes);
            //读取消息ID
            byte[] msgIdBytes = new byte[4];
            Array.Copy(totalBuffer, 8, msgIdBytes, 0, 4);
            unPackageObject.msgId = ByteConvert.Bytes2UInt(msgIdBytes);
            //读取消息体
            byte[] bodyBytes = new byte[totalLength - 12];
            Array.Copy(totalBuffer, 12, bodyBytes, 0, totalLength - 12);
            unPackageObject.body = ByteConvert.ByteToObj(bodyBytes, bodyBytes.Length);

            return unPackageObject;

        }
    }
}
