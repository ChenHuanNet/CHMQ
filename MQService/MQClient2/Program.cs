using Newtonsoft.Json;
using SocketHelper;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MQClient2
{
    public class Msg
    {
        /// <summary>
        /// 主题
        /// </summary>
        public string topic;

        /// <summary>
        /// 标签，可用于 主题 下的消息分类，即分层
        /// </summary>
        public string tag;

        /// <summary>
        /// 消息Key
        //设置代表消息的业务关键属性，请尽可能全局唯一，以方便您在无法正常收到消息情况下，可通过MQ控制台查询消息并补发
        // 注意：不设置也不会影响消息正常收发
        /// </summary>
        public string key;

        /// <summary>
        /// 消息ID
        /// </summary>
        public string msgId;

        /// <summary>
        /// 消息内容体
        /// </summary>
        public string body;

        //public override string ToString()
        //{
        //    return string.Format("msgId:{0},body:{1}", msgId, body);
        //}

        public override string ToString() => $"topic:{topic},tag:{tag},key:{key},msgId:{msgId},body:{body}";
    }
    class Program
    {
        static void Main(string[] args)
        {
            int[] arr = new int[] { 1, 2, 3, 4, 5 };
            int fan = arr[^1];// 输出3 反向
            int[] fanwei = arr[1..^2];// 输出[2,3] 范围索引 包括前面不包括后面

            //SequenceReader<Msg> sequenceReader = new System.Buffers.SequenceReader();


            // http://47.98.182.216:8012/transparentSendAPI/send,{"device_id":"112206006","msgType":251,"msgFormat":"txt","msgContent":"{\"SendType\":\"unicast\",\"MsgType\":\"text\",\"Content\":\"测试1111111111\",\"PicUrl\":\"\",\"VoiceUrl\":\"\",\"VideoUrl\":\"\",\"LinkUrl\":\"\",\"Payload\":\"{\\\"content\\\":\\\"测试1111111111\\\",\\\"duration\\\":0}\",\"DeviceIds\":\"60012014\",\"Channel\":\"\",\"SendTime\":1571103410,\"CmdId\":\"[cmdid]\"}","md5Hex":"489b5e6f6364bd12c4d902c093aeda1f"} 
            try
            {
                string device_id = "112206006";
                string content = "{\"SendType\":\"unicast\",\"MsgType\":\"text\",\"Content\":\"测试1111111111\",\"PicUrl\":\"\",\"VoiceUrl\":\"\",\"VideoUrl\":\"\",\"LinkUrl\":\"\",\"Payload\":\"{\\\"content\\\":\\\"测试1111111111\\\",\\\"duration\\\":0}\",\"DeviceIds\":\"60012014\",\"Channel\":\"\",\"SendTime\":1571103410,\"CmdId\":\"[cmdid]\"}";
                HttpClient httpClient = new HttpClient();
                var url = "http://47.98.182.216:8012";
                var msgType = 251;
                var msgFormat = "txt";
                var value = device_id + msgType.ToString() + msgFormat + content;
                var md5 = "489b5e6f6364bd12c4d902c093aeda1f";
                string requestUrl = $"{url}/transparentSendAPI/send";
                Dictionary<string, object> requestParams = new Dictionary<string, object>();
                requestParams.Add("device_id", device_id);
                requestParams.Add("msgType", msgType);
                requestParams.Add("msgFormat", msgFormat);
                requestParams.Add("msgContent", content);
                requestParams.Add("md5Hex", md5);
                var body = Newtonsoft.Json.JsonConvert.SerializeObject(requestParams);
                //string requestParam = $"device_id={device_id}&msgType={msgType}&msgFormat={msgFormat}&msgContent={content}&md5Hex={md5}";

                var httpContent = new StringContent(body, Encoding.UTF8, "application/json");
                var task = httpClient.PostAsync(requestUrl, httpContent).Result;
                var ret2 = task.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {

                throw;
            }


            Console.WriteLine($"This is a Client2");

            string ts = ((long)(DateTime.Now.Date.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds).ToString();
            Console.WriteLine(ts);

            List<Msg> msgs = new List<Msg>();
            msgs.Add(new Msg() { topic = "topic", tag = "tag", key = "key", msgId = "1234", body = "11111" });
            msgs.Add(new Msg() { topic = "topic", tag = "tag", key = "key", msgId = "1234", body = "11111" });
            msgs.Add(new Msg() { topic = "topic", tag = "tag1", key = "key1", msgId = "123", body = "11111" });
            msgs.Add(new Msg() { topic = "topic", tag = "tag1", key = "key1", msgId = "123", body = "11111" });
            msgs.Add(new Msg() { topic = "topi2", tag = "tag2", key = "key1", msgId = "12", body = "11111" });

            Msg[] msgs2 = msgs.ToArray();

            Msg[] msgs3 = msgs2.GroupBy(x => x.msgId).Select(x => x.FirstOrDefault()).ToArray();


            //List<int> msgs = new List<int>();
            //for (int i = 0; i < 100000; i++)
            //{
            //    msgs.Add(i);
            //}

            ////这里面方法是异步的 ,但是  对外层的程序而言是同步的
            //Parallel.ForEach(msgs, (msg) =>
            //{
            //    Console.WriteLine(msg);
            //});
            //Console.WriteLine("-----------");

            string sss1 = "Here is a Phone Number 111-2313 and 133-2311";
            string sss2 = "(111)51779 is UID01的电话号码";
            string sss3 = "0882-267|0101.2345|SYN23127088";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(sss1);
            sb.AppendLine(sss2);
            sb.AppendLine(sss3);

            string sss4 = sb.ToString();

            string str1 = @"Here is a Phone Number ([\s\S]*?)\-([\s\S]*?) and ([\s\S]*?)\-([\s\S]*?)";
            string str2 = @"\(([\s\S]*?)\)([\s\S]*?) is UID01的电话号码";
            string str3 = @"([\s\S]*?)\-([\s\S]*?)\|([\s\S]*?)\.([\s\S]*?)\|SYN23127088";

            sb = new StringBuilder();
            sb.AppendLine(str1);
            sb.AppendLine(str2);
            sb.AppendLine(str3);

            string str4 = sb.ToString();

            List<string> list4 = Match(str4, sss4, 10);

            sb = new StringBuilder();
            foreach (var item in list4)
            {
                sb.Append(item);
            }

            string result = sb.ToString();

            //string str = "";
            //string path = AppDomain.CurrentDomain.BaseDirectory + "/Data/20190711card.txt";
            //byte[] bs = null;
            //using (FileStream fs = new FileStream(path, FileMode.Open))
            //{
            //    bs = new byte[fs.Length];
            //    fs.Read(bs, 0, bs.Length);
            //}
            //str = "[" + Encoding.UTF8.GetString(bs) + "]";

            //List<string> list = JsonConvert.DeserializeObject<List<string>>(str);
            //list.Sort();

            //AsynchronousClient asynchronousClient = new AsynchronousClient(10000);
            //asynchronousClient.msgReceiveEvent += Handle;
            //asynchronousClient.StartClient();

            //Subscribe(asynchronousClient);

            //Thread.Sleep(3000);

            //DisSubscribe(asynchronousClient);

            //while (true)
            //{
            //    var input = Console.ReadLine();
            //    if (input == "1")
            //    {
            //        Subscribe(asynchronousClient);
            //    }
            //    else if (input == "2")
            //    {
            //        Publish(asynchronousClient);
            //    }
            //    else if (input == "exit")
            //    {
            //        break;
            //    }
            //}

            Console.ReadLine();
        }

        /// <summary>
        /// 通过正则表达式查找匹配到的字符串保存在List<string>
        /// </summary>
        /// <param name="parm">正则表达式</param>
        /// <param name="str">待寻找的字符串</param>
        /// <returns></returns>
        public static List<string> Repex(string parm, string str)
        {
            List<string> list = new List<string>();
            var regex = new Regex(parm, RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var math = regex.Matches(str);
            if (math.Count < 0) return list;

            foreach (var item in math)
            {
                var strdetails = (item as Match).Value;

                list.Add(strdetails);
            }

            return list;

        }

        /// <summary>
        /// 正则表达式匹配参数 ([\s\S]*?) 表示匹配任意字符
        ///  示例  var item = f_Regex.Match(@"<div class="" item-last item-item ([\s\S]*?)>([\s\S]*?)<div class=""p-name"">([\s\S]*?)</div>([\s\S]*?)<div class=""cell p-price"">([\s\S]*?)</strong>([\s\S]*?)<div class=""cell p-sum"">([\s\S]*?)</div>([\s\S]*?)", a, 8);
        /// </summary>
        /// <param name="parm">正则表达式</param>
        /// <param name="str">待匹配参数的字符串</param>
        /// <param name="matchnum">存储的匹配参数个数</param>
        /// <returns></returns>
        public static List<string> Match(string parm, string str, int matchnum)
        {
            List<string> list = new List<string>();
            var math = Regex.Matches(str, parm);
            if (math.Count < 0) return list;
            for (int i = 0; i < matchnum; i++)
            {
                list.Add(math[0].Result("$" + (i + 1)));
            }
            return list;

        }

        private static void Subscribe(AsynchronousClient asynchronousClient)
        {
            SubscribeObject subscribeObject = new SubscribeObject();
            subscribeObject.topic = "test";
            Console.WriteLine($"我订阅了主题[{subscribeObject.topic}]");
            asynchronousClient.Send(subscribeObject, MsgOperation.订阅消息Socket方式);

        }

        private static void DisSubscribe(AsynchronousClient asynchronousClient)
        {
            SubscribeObject subscribeObject = new SubscribeObject();
            subscribeObject.topic = "test";
            Console.WriteLine($"我订阅了主题[{subscribeObject.topic}]");
            asynchronousClient.Send(subscribeObject, MsgOperation.取消订阅);

        }

        private static void Publish(AsynchronousClient asynchronousClient)
        {
            PublishObject publishObject = new PublishObject();
            publishObject.topic = "test";
            publishObject.content = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + new Random().Next(999).ToString();
            Console.WriteLine($"我在主题[{publishObject.topic}]发布了一条消息:{publishObject.content}");
            asynchronousClient.Send(publishObject, MsgOperation.发布广播);
        }

        /// <summary>
        /// 接完到完整消息后处理消息
        /// </summary>
        /// <param name="state"></param>
        private static void Handle(UnPackageObject obj)
        {
            string result = JsonConvert.SerializeObject(obj.body);
            Console.WriteLine(obj.ope.ToString() + ":" + result);
        }
    }
}
