using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Sgml;
using System.Web;
using System.IO;

namespace Nichan
{
    class ThreadParserException : NichanException
    {
        public ThreadParserException() : base() { }
        public ThreadParserException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// 2chのスレッドを解析するパーサ
    /// </summary>
    interface IThreadParser
    {
        Thread Parse(XDocument doc);
    }

    /// <summary>
    /// 1行目が&lt;!DOCTYPE html&gt;から始まり、&lt;meta name="viewport" /&gt;が設定されていないスレ用パーサ
    /// </summary>
    class Type1ThreadParser :IThreadParser
    {
        static private readonly Regex reDate = new Regex(@"(\d+)/(\d+)/(\d+)[^ ]* (\d+):(\d+):(\d+)\.(\d+)");
        private static DateTime? getDate(string str)
        {
            Match m = reDate.Match(str);
            if (m.Success)
                return new DateTime(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value),
                    int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value), int.Parse(m.Groups[6].Value), int.Parse(m.Groups[7].Value) * 10, DateTimeKind.Local);
            else
                return null;
        }

        public Thread Parse(XDocument doc)
        {
            var ret = new Thread();
            ret.Title = HttpUtility.HtmlDecode( doc.XPathSelectElement(@"/html/body/h1[@class=""title""]")?.Value);
            if (ret.Title == null) throw new ThreadParserException();
            foreach(XElement elem in doc.XPathSelectElements(@"/html/body/div[@class=""thread""]/div[@class=""post""]"))
            {
                var res = new Res();
                res.Number = (int)elem.Attribute("id");
                res.Name = HttpUtility.HtmlDecode(elem.XPathSelectElement(@"div[@class=""name""]/b").Value);
                res.Mail = HttpUtility.HtmlDecode(elem.XPathSelectElement(@"div[@class=""name""]/b/a")?.Attribute("href").Value);
                string dateStr = elem.XPathSelectElement(@"div[@class=""date""]").Value;
                int idx = dateStr.IndexOf("ID:");
                res.UserId=idx!=-1 ? dateStr.Substring(idx + 3):null;
                res.Date = getDate(dateStr);
                res.Text = elem.XPathSelectElement(@"div[@class=""message""]");
                ret.Res.Add(res);
            }
            ret.ResCount = ret.Res.Count;
            return ret;
        }
    }

    /// <summary>
    /// 1行目が&lt;html prefix="og: http://ogp.me/ns#"&gt;から始まるスレ用パーサ
    /// </summary>
    class Type2ThreadParser:IThreadParser
    {
        static private readonly Regex reDate = new Regex(@"(\d+)/(\d+)/(\d+)[^ ]* (\d+):(\d+):(\d+)\.(\d+)");
        private static DateTime? getDate(string str)
        {
            Match m = reDate.Match(str);
            if (m.Success)
                return new DateTime(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value),
                    int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value), int.Parse(m.Groups[6].Value), int.Parse(m.Groups[7].Value) * 10, DateTimeKind.Local);
            else
                return null;
        }

        public Thread Parse(XDocument doc)
        {
            var ret = new Thread();
            ret.Title = HttpUtility.HtmlDecode(doc.XPathSelectElement(@"/html/body/div/span/h1")?.Value);
            if (ret.Title == null) throw new ThreadParserException();
            
            foreach(XElement elem in doc.XPathSelectElements(@"/html/body/div/span/div/dl[@class=""thread""]/dt"))
            {
                var res = new Res();
                string str = (string)elem.XPathEvaluate(@"string(text()[1])");
                res.Number=int.Parse(str.Substring(0, str.IndexOf(' ')));
                res.Name = HttpUtility.HtmlDecode(elem.XPathSelectElement(@"//b").Value);
                res.Mail = elem.Element("a")?.Attribute("href")?.Value;
                if (res.Mail != null)
                    res.Mail = HttpUtility.HtmlDecode(res.Mail);
                str=(string)elem.XPathEvaluate(@"string(text()[last()])");
                int idx=str.IndexOf("ID:");
                res.UserId = idx == -1 ? null : str.Substring(idx + 3);
                res.Date = getDate(str);
                res.Text = elem.XPathSelectElement(@"following-sibling::dd[1]");
                XElement[] brs = res.Text.Elements("br").ToArray();
                brs[brs.Length - 2].Remove();
                brs[brs.Length - 1].Remove();
                ret.Res.Add(res);
            }
            ret.ResCount = ret.Res.Count;
            return ret;
        }
    }

    /// <summary>
    /// 1行目が&lt;!DOCTYPE html&gt;から始まり、&lt;meta name="viewport" /&gt;が設定されているスレ用パーサ
    /// </summary>
    class Type3ThreadParser : IThreadParser
    {
        static private readonly Regex reDate = new Regex(@"(\d+)/(\d+)/(\d+)[^ ]* (\d+):(\d+):(\d+)\.(\d+)");
        private static DateTime? getDate(string str)
        {
            Match m = reDate.Match(str);
            if (m.Success)
                return new DateTime(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value),
                    int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value), int.Parse(m.Groups[6].Value), int.Parse(m.Groups[7].Value) * 10, DateTimeKind.Local);
            else
                return null;
        }

        public Thread Parse(XDocument doc)
        {
            var ret = new Thread();
            ret.Title = HttpUtility.HtmlDecode(doc.XPathSelectElement(@"/html/head/title")?.Value);
            if (ret.Title == null) throw new ThreadParserException();

            foreach (XElement elem in doc.XPathSelectElements(@"/html/body/div/div[@class=""thread""]/div[@class=""post""]"))
            {
                var res = new Res();
                res.Number = (int)elem.Attribute("id");
                res.Name = HttpUtility.HtmlDecode(elem.XPathSelectElement(@"div[@class=""meta""]/span[@class=""name""]/b").Value);
                res.Mail = HttpUtility.HtmlDecode(elem.XPathSelectElement(@"div[@class=""meta""]/span[@class=""name""]/b/a")?.Attribute("href").Value);
                res.UserId = elem.Attribute("data-userid").Value;
                if (res.UserId.Length > 3) res.UserId = res.UserId.Substring(3);
                string dateStr = elem.XPathSelectElement(@"div[@class=""meta""]/span[@class=""date""]").Value;
                res.Date = getDate(dateStr);
                res.Text = elem.XPathSelectElement(@"div[@class=""message""]/span");
                ret.Res.Add(res);
            }
            ret.ResCount = ret.Res.Count;
            return ret;
        }
    }

    public static class ThreadParser
    {
        private static IThreadParser GetThreadParser(XDocument doc)
        {
            if (doc.Element("html")?.Attribute("prefix") == null)
            {
                if (doc.XPathSelectElement(@"/html/head/meta[@name=""viewport""]") == null)
                    return new Type1ThreadParser();
                else
                    return new Type3ThreadParser();
            }
            else
                return new Type2ThreadParser();
        }

        /// <summary>
        /// スレッドのHTMLを格納した<see cref="TextReader"/>からスレッドを解析。
        /// ただし<see cref="Thread.Uri"/>および<see cref="Thread.Name"/>はnull。
        /// </summary>
        /// <exception cref="ThreadParserException">解析エラーの場合</exception>
        public static Thread ParseFromStream(TextReader reader)
        {
            using var sgmlReader = new SgmlReader { DocType = "HTML", IgnoreDtd = false, InputStream = reader };
            sgmlReader.WhitespaceHandling = WhitespaceHandling.None;
            sgmlReader.CaseFolding = CaseFolding.ToLower;
            var doc = XDocument.Load(sgmlReader);

            Thread ret = GetThreadParser(doc).Parse(doc);
            return ret;
        }
    }
}
