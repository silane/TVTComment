// 参考: http://info.5ch.net/index.php/Monazilla/develop/dat

using Sgml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;


namespace Nichan
{
    public class DatParserException : NichanException
    {
        public DatParserException() { }
        public DatParserException(string message) : base(message) { }
        public DatParserException(string message, Exception inner) : base(message, inner) { }
    }

    public class DatParser
    {
        /// <summary>
        /// 解析結果のスレタイが入る。解析前の場合は<c>null</c>。
        /// </summary>
        public string ThreadTitle { get; private set; }

        public DatParser()
        {
            this.Reset();
        }

        /// <summary>
        /// DAT文字列を追加し解析する
        /// </summary>
        /// <exception cref="DatParserException"></exception>
        public void Feed(string datString)
        {
            this.buffer += datString;
            if (this.buffer == "")
                return;
            var rows = this.buffer.Split('\n');
            var reses = rows[..^1].Select(x => x.Split("<>")).ToArray();
            this.buffer = rows[^1];

            if (reses.Any(x => x.Length != 5))
            {
                throw new DatParserException();
            }

            if(this.ThreadTitle == null && reses.Length > 0)
            {
                this.ThreadTitle = reses[0][4];
            }

            foreach(var row in reses)
            {
                int startIdx = row[2].IndexOf("ID:");
                string userId = startIdx != -1 ? row[2][(startIdx + 3)..] : null;

                XElement text;
                using (StringReader reader = new StringReader($"<html><div>{row[3]}</div></html>"))
                {
                    using (var sgml = new SgmlReader { DocType = "HTML", IgnoreDtd = false, InputStream = reader })
                    {
                        sgml.WhitespaceHandling = WhitespaceHandling.All;
                        sgml.CaseFolding = Sgml.CaseFolding.ToLower;
                        text = XDocument.Load(sgml).XPathSelectElement("./html/div");
                    }
                }

                this.reses.Enqueue(new Res
                {
                    Number = ++this.resNum,
                    Name = HttpUtility.HtmlDecode(row[0]),
                    Mail = HttpUtility.HtmlDecode(row[1]),
                    UserId = userId,
                    Date = getDate(row[2]),
                    Text = text,
                });
            }
        }

        /// <summary>
        /// 解析結果のレスのキューからレスをポップして返す
        /// </summary>
        public Res PopRes()
        {
            return this.reses.TryDequeue(out Res res) ? res : null;
        }

        /// <summary>
        /// パーサの状態を初期化する
        /// </summary>
        public void Reset()
        {
            this.ThreadTitle = null;
            this.buffer = "";
            this.reses.Clear();
            this.resNum = 0;
        }

        private string buffer;
        private Queue<Res> reses = new Queue<Res>();
        private int resNum;

        private static readonly Regex reDate = new Regex(@"(\d+)/(\d+)/(\d+)[^ ]* (\d+):(\d+):(\d+)\.(\d+)");
        private static DateTime? getDate(string str)
        {
            Match m = reDate.Match(str);
            if (m.Success)
                return new DateTime(
                    int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value),
                    int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value), int.Parse(m.Groups[6].Value), int.Parse(m.Groups[7].Value) * 10, DateTimeKind.Local
                );
            else
                return null;
        }
    }
}
