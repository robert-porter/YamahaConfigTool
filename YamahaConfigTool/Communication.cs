using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.IO;
using System.Net;
using System.Linq;
using System.Xml.XPath;
using System.Threading;
using System.Net.Sockets;

namespace YamahaConfigTool
{
    public class Communication
    {

        public String HostNameOrAddress = "192.168.1.126";
        public String Port = "80";

        public XmlDocument SendCommand(String command)
        {

            if (command == null)
                return null;

            //TODO: return status better

            String relativeUri = "YamahaRemoteControl/ctrl";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("http://{0}:{1}/{2}", HostNameOrAddress, Port, relativeUri));

            request.UserAgent = @"AVcontrol";
            request.ContentType = @"text/xml; charset=UTF-8";
            request.Method = "POST";

            byte[] body = Encoding.UTF8.GetBytes(command);

            request.ContentLength = body.LongLength;

            try
            {

                using (Stream s = request.GetRequestStream())
                {
                    s.Write(body, 0, body.Length);
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    String result = sr.ReadToEnd();
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(result);

                    int rc = int.Parse(doc.DocumentElement.GetAttribute("RC"));

                    if (rc == 0) // command was successful
                    {
                        return doc;
                    }
                    else
                    { //TODO: set on status bar or something
                        // System.Console.WriteLine(Command);
                        // System.Console.WriteLine("The receiver has returned an error: YAMAHA_AV RC={0}", rc);
                    }
                }
            }

            catch (System.Net.WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpStatusCode sc = ((HttpWebResponse)e.Response).StatusCode;
                    if (sc == HttpStatusCode.BadRequest)
                    {
                        System.Console.WriteLine("Receiver has returned a Bad Request");
                        System.Console.WriteLine(command);
                    }
                }
                if (e.Status == WebExceptionStatus.ConnectFailure)
                {
                    // TODO Specify connection error and set on status bar or something.
                    return null;
                }
            }

            return null;

        }
    }
}
