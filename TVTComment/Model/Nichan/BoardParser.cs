using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Sgml;
using System.Net.Http;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Nichan
{
    /// <summary>
    /// 2chの板を解析するパーサ
    /// </summary>
    interface IBoardParser
    {
        Board Parse(XDocument doc,Uri docUri);
    }

    class Type1BoardParser:IBoardParser
    {
        public Board Parse(XDocument doc,Uri docUri)
        {
            try
            {
                var ret = new Board();
                string baseUri = doc.XPathSelectElement(@"/html/head/base").Attribute("href").Value;

                foreach (XElement elem in doc.XPathSelectElements(@"/html/body/div[2]/small/a"))
                {
                    var thread = new Thread();
                    string str = elem.Value;
                    int idx = str.IndexOf(' ') + 1;
                    int idx2 = str.LastIndexOf(' ');
                    thread.Title = str.Substring(idx, idx2 - idx);
                    thread.Uri = new Uri(docUri, baseUri);
                    thread.Uri = new Uri(thread.Uri, elem.Attribute("href").Value);
                    thread.ResCount = int.Parse(str.Substring(idx2 + 2, str.LastIndexOf(')') - idx2 - 2));
                    ret.Threads.Add(thread);
                }
                return ret;
            }catch(NullReferenceException e)
            {
                throw new InternalParseException(null,e);
            }
            catch(ArgumentOutOfRangeException e)
            {
                throw new InternalParseException(null, e);
            }
        }
    }

    public static class BoardParser
    {
        private static HttpClient httpClient = new HttpClient();

        private static IBoardParser GetBoardParser(XDocument doc)
        {
            return new Type1BoardParser();
        }

        public static Board ParseFromUri(string uri)
        {
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            WebResponse response = request.GetResponse();
            Uri resolvedUri = response.ResponseUri;//リダイレクトされた場合を考えて
            
            XDocument doc;
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("shift_jis")))//とりあえずshiftjis固定
            {
                using (var sgml = new SgmlReader { DocType = "HTML", IgnoreDtd = false, InputStream = reader })
                {
                    sgml.WhitespaceHandling = WhitespaceHandling.All;
                    sgml.CaseFolding = Sgml.CaseFolding.ToLower;
                    doc = XDocument.Load(sgml);
                }
            }
            Board ret;
            try
            {
                ret = GetBoardParser(doc).Parse(doc, resolvedUri);
            }
            catch(InternalParseException e)
            {
                throw new ParseException(resolvedUri, e);
            }
            ret.Uri = resolvedUri;
            return ret;
        }
    }
}
