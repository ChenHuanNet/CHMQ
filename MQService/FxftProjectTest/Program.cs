using System;

namespace FxftProjectTest
{
    class Program
    {
        static void Main(string[] args)
        {

            string xmlString = "<root><web:NEW_DATA_TICKET_QRsp xmlns:web=\"http://www.example.org/webservice\"><CHARGE_CNT_CH>0元</CHARGE_CNT_CH><DURATION_CNT_CH></DURATION_CNT_CH><IRESULT>0</IRESULT><TOTALCOUNT></TOTALCOUNT><TOTALPAGE></TOTALPAGE><TOTAL_BYTES_CNT>0.00MB</TOTAL_BYTES_CNT><GROUP_TRANSACTIONID>1000000190201909030824413635</GROUP_TRANSACTIONID><number>1410028214051</number></web:NEW_DATA_TICKET_QRsp></root>";


            var mb = XmlHelper.XmlParse(xmlString, "TOTAL_BYTES_CNT");

            Console.WriteLine("Hello World!");
            Console.Read();
        }
    }
}
