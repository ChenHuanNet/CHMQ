using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace FxftProjectTest
{
    public class XmlHelper
    {
        /// <summary>
        /// 解析xml字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlString"></param>
        /// <returns></returns>
        public static T XmlDeserialize<T>(string xmlString)
        {
            T t = default(T);
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                using (Stream xmlStream = new MemoryStream(Encoding.UTF8.GetBytes(xmlString)))
                {
                    using (XmlReader xmlReader = XmlReader.Create(xmlStream))
                    {
                        t = (T)xmlSerializer.Deserialize(xmlReader);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return t;
        }

        public static string XmlParse(string xml, string tagName)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            var xnodes = doc.GetElementsByTagName(tagName);
            if (xnodes != null && xnodes.Count > 0)
            {
                return xnodes[0].InnerText.Trim();
            }
            return null;
        }
    }
}
