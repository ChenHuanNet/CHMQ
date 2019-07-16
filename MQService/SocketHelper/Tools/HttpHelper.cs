using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SocketHelper
{
    public class HttpHelper
    {
        public static async Task<string> PostJsonData(string url, string json)
        {
            string response = string.Empty;
            //发送Http消息
            var handler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            HttpClient httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.Timeout = new TimeSpan(0, 0, 30);

            HttpContent httpContent = new StringContent(json);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            httpContent.Headers.ContentType.CharSet = "utf-8";
            try
            {
                var task = httpClient.PostAsync(url, httpContent).ConfigureAwait(false);
                //一直显示无效的 时间戳  尚未联调
                response = await task.GetAwaiter().GetResult().Content.ReadAsStringAsync();
            }
            catch (TaskCanceledException ex)
            {

            }

            return response;
        }
    }
}
